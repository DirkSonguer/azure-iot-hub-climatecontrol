﻿#pragma checksum "C:\Projects\azure-iot-hub-climatecontrol\device\DeviceApplication\MainPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "C3869037D4A43CB22F4E18E9F2F90128"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DeviceApplication
{
    partial class MainPage : 
        global::Windows.UI.Xaml.Controls.Page, 
        global::Windows.UI.Xaml.Markup.IComponentConnector,
        global::Windows.UI.Xaml.Markup.IComponentConnector2
    {
        /// <summary>
        /// Connect()
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 10.0.18362.1")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 2: // MainPage.xaml line 13
                {
                    this.uiAppVersionOut = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 3: // MainPage.xaml line 47
                {
                    global::Windows.UI.Xaml.Controls.Button element3 = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)element3).Click += this.SendData_Button_Click;
                }
                break;
            case 4: // MainPage.xaml line 44
                {
                    this.uiDataTransferOut = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 5: // MainPage.xaml line 40
                {
                    this.uiApplicationOut = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 6: // MainPage.xaml line 35
                {
                    this.uiCurrentLoudnessOut = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 7: // MainPage.xaml line 31
                {
                    this.uiCurrentBrightnessOut = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 8: // MainPage.xaml line 27
                {
                    this.uiCurrentHumidityOut = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 9: // MainPage.xaml line 23
                {
                    this.uiCurrentTemperatureOut = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 10: // MainPage.xaml line 19
                {
                    this.uiCurrentTimeOut = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            default:
                break;
            }
            this._contentLoaded = true;
        }

        /// <summary>
        /// GetBindingConnector(int connectionId, object target)
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 10.0.18362.1")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::Windows.UI.Xaml.Markup.IComponentConnector GetBindingConnector(int connectionId, object target)
        {
            global::Windows.UI.Xaml.Markup.IComponentConnector returnValue = null;
            return returnValue;
        }
    }
}

