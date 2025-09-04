#region MIT License

// ==========================================================
// 
// SCRM project - Copyright (c) 2024 JeePeeTee
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// 
// ===========================================================

#endregion

#region usings

using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Xpo;
using XAF.MFA.Module.BusinessObjects;

#endregion

namespace XAF.MFA.Module.Controllers.Models;

[DomainComponent]
public class GuardCodeEntry : INotifyPropertyChanged {
    private string _guardCode;
    private string _hiddenGuardCode;

    public GuardCodeEntry() {
        HiddenGuardCode = GenerateRandomCode();
        GuardCodeTimeout = DateTime.Now.AddMinutes(10);
        EmailUser(HiddenGuardCode);
    }

    public string GuardCode {
        get => this._guardCode;
        set {
            this._guardCode = value;
            this.RaisePropertyChanged(nameof(GuardCode));
        }
    }

    [Browsable(false)]
    public DateTime GuardCodeTimeout { get; }

    [Browsable(false)]
    public bool GuardCodeCorrect => this.GuardCode?.ToUpper() == this.HiddenGuardCode && DateTime.Now < this.GuardCodeTimeout;

    [Browsable(false)]
    public string HiddenGuardCode {
        get => this._hiddenGuardCode;
        set {
            this._hiddenGuardCode = value;
            this.RaisePropertyChanged(nameof(HiddenGuardCode));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void RaisePropertyChanged(string propertyName) {
        if (this.PropertyChanged == null)
            return;
        this.PropertyChanged((object)this, new PropertyChangedEventArgs(propertyName));
    }

    private static string GenerateRandomCode() {
        // Define the allowed characters (letters and digits, excluding 'I', '1', 'O', '0')
        const string allowedChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rand = new Random();
        const int length = 6;

        var randomString = new char[length];
        for (var i = 0; i < length; i++) {
            randomString[i] = allowedChars[rand.Next(allowedChars.Length)];
        }

        return new string(randomString);
    }

    private static void EmailUser(string guardCode) {
        var currentUser = (ApplicationUser)SecuritySystem.CurrentUser;
        var ios = XPObjectSpace.FindObjectSpaceByObject(currentUser);

        // Email guardCode to currentUser here...
    }
}