using System.ComponentModel;
using DevExpress.CodeParser.Diagnostics;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Updating;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.DomainLogics;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Base.Security;
using DevExpress.Persistent.BaseImpl;
using XAF.MFA.Module.BusinessObjects;
using XAF.MFA.Module.Controllers.Models;

namespace XAF.MFA.Blazor.Server;

[ToolboxItemFilter("Xaf.Platform.Blazor")]
// For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.ModuleBase.
public sealed class MFABlazorModule : ModuleBase {
    
    private const string ShouldEnterGuardCodeKeyName = "user should enter GuardCode on logon";

    private readonly PopupWindowShowAction _requestGuardCode;
    private uint _guardCodeRetries = 0;
    private bool _isChangePasswordOnLogonExecuted = false;
    
    //void Application_CreateCustomModelDifferenceStore(object sender, CreateCustomModelDifferenceStoreEventArgs e) {
    //    e.Store = new ModelDifferenceDbStore((XafApplication)sender, typeof(ModelDifference), true, "Blazor");
    //    e.Handled = true;
    //}
    void Application_CreateCustomUserModelDifferenceStore(object sender, CreateCustomModelDifferenceStoreEventArgs e) {
        e.Store = new ModelDifferenceDbStore((XafApplication)sender, typeof(ModelDifference), false, "Blazor");
        e.Handled = true;
    }
    public MFABlazorModule() {
        _requestGuardCode = new PopupWindowShowAction(null, "EnterGuardCode", "");
        _requestGuardCode.Execute += new PopupWindowShowActionExecuteEventHandler(enterGuardCodeOnLogon_OnExecute);
        _requestGuardCode.Cancel += new EventHandler(enterGuardCodeOnLogon_OnCancel);
        _requestGuardCode.CustomizePopupWindowParams += new CustomizePopupWindowParamsEventHandler(enterGuardCodeOnLogon_OnCustomizePopupWindowParams);

    }

    private void enterGuardCodeOnLogon_OnCustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs args) {
        var objectSpace = Application.CreateObjectSpace(typeof(GuardCodeEntry));
        args.View = _requestGuardCode.Application.CreateDetailView(objectSpace, new GuardCodeEntry());
        args.View.Closing += View_Closing;
        ((DetailView)args.View).ViewEditMode = ViewEditMode.Edit;
        args.Context = TemplateContext.PopupWindow;
        var applicationType = Application.GetType();
        if (applicationType is null) return;

        while (applicationType != typeof(object)) {
            if (applicationType.Name == "WebApplication" || applicationType.Name == "BlazorApplication") {
                args.DialogController.CancelAction.Active["NoCancelInChangePasswordView"] = false;
                break;
            }

            applicationType = applicationType.BaseType;
        }

    }

    private void View_Closing(object sender, EventArgs e) {
        if (!_isChangePasswordOnLogonExecuted) {
            _requestGuardCode.Application.Exit();
        }
    }

    private void enterGuardCodeOnLogon_OnCancel(object sender, EventArgs e) {
        _requestGuardCode.Application.Exit();
        _isChangePasswordOnLogonExecuted = true;
    }

    private void enterGuardCodeOnLogon_OnExecute(object sender, PopupWindowShowActionExecuteEventArgs args) {
        Guard.ArgumentNotNull(args.PopupWindow, "args.PopupWindow");
        Guard.ArgumentNotNull(args.PopupWindow, "args.PopupWindow.View");
        ChangePasswordOnLogon((GuardCodeEntry)args.PopupWindow.View.CurrentObject);
        _isChangePasswordOnLogonExecuted = true;
        _requestGuardCode.Active.SetItemValue(ShouldEnterGuardCodeKeyName, false);

    }
    
    private void ChangePasswordOnLogon(GuardCodeEntry guardCodeParameters) {
        Guard.ArgumentNotNull(guardCodeParameters, "guardCodeParameters");
        Guard.ArgumentNotNull(Application, "Application");
        Guard.ArgumentNotNull(SecuritySystem.CurrentUser, "SecuritySystem.CurrentUser");

        using var objectSpace = Application.CreateObjectSpace(SecuritySystem.CurrentUser.GetType());
        var user = (ApplicationUser)objectSpace.GetObject(SecuritySystem.CurrentUser);
        if (user == null) {
            var exception = new InvalidOperationException(SecurityExceptionLocalizer.GetExceptionMessage(SecurityExceptionId.UnableToReadCurrentUserData,
                ((IAuthenticationStandardUser)SecuritySystem.CurrentUser).UserName));
            if (!Tracing.LogSensitiveData && !LogSensitiveDataSettings.ChangePasswordOnLogonException) {
                object securedRepresentation;
                var id = TypeInfo.TryGetSecuredRepresentation(SecuritySystem.CurrentUser, Application?.TypesInfo,
                    out securedRepresentation)
                    ? securedRepresentation.ToString()
                    : Tracing.SensitiveDataReplacement;
                exception.Data[Tracing.TracingExceptionMessage] =
                    SecurityExceptionLocalizer.GetExceptionMessage(SecurityExceptionId.UnableToReadCurrentUserData, id);
            }

            throw exception;
        }

        if (guardCodeParameters.GuardCodeCorrect) return;

        _guardCodeRetries++;
        if (_guardCodeRetries < 3)
            throw new UserFriendlyException("Guardcode is invalid. Try again!");

        _requestGuardCode.Application.LogOff();
    }


    public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB) {
        return ModuleUpdater.EmptyModuleUpdaters;
    }
    public override void Setup(XafApplication application) {
        base.Setup(application);
        // Uncomment this code to store the shared model differences (administrator settings in Model.XAFML) in the database.
        // For more information, refer to the following topic: https://docs.devexpress.com/eXpressAppFramework/113698/
        //application.CreateCustomModelDifferenceStore += Application_CreateCustomModelDifferenceStore;
        application.CreateCustomUserModelDifferenceStore += Application_CreateCustomUserModelDifferenceStore;
    }
    
    public override IList<PopupWindowShowAction> GetStartupActions() {
        var user = SecuritySystem.CurrentUser as ApplicationUser;

        // if (user is not { MfaEnabled: true }) return base.GetStartupActions();

        _requestGuardCode.Active.SetItemValue("Request GuardCode is supported by security", IsSupportGuardCode());
        var securityStrategyBase = Application?.Security;
        var standardUser = securityStrategyBase?.User as ApplicationUser;
        var doEnterGuardCodeOnLogon = standardUser != null; // && standardUser.MfaEnabled;
        _requestGuardCode.Active.SetItemValue(ShouldEnterGuardCodeKeyName, doEnterGuardCodeOnLogon);
        return [_requestGuardCode];
    }
    
    private bool IsSupportGuardCode() {
        return true;

        // var supportChangePasswordOption = Application?.Security as ISupportChangePasswordOption;
        // return supportChangePasswordOption != null && supportChangePasswordOption.IsSupportChangePassword;
    }


}
