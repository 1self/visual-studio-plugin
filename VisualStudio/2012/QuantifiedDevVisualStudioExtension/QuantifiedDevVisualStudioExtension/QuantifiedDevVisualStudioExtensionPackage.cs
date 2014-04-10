using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace QuantifiedDev.QuantifiedDevVisualStudioExtension
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(MyToolWindow))]
    [ProvideEditorExtension(typeof(EditorFactory), ".quantifieddevvisualstudioextension", 50, 
              ProjectGuid = "{A2FE74E1-B743-11d0-AE1A-00A0C90FFFC3}", 
              TemplateDir = "Templates", 
              NameResourceID = 105,
              DefaultName = "QuantifiedDevVisualStudioExtension")]
    [ProvideKeyBindingTable(GuidList.guidQuantifiedDevVisualStudioExtensionEditorFactoryString, 102)]
    // Our Editor supports Find and Replace therefore we need to declare support for LOGVIEWID_TextView.
    // This attribute declares that your EditorPane class implements IVsCodeWindow interface
    // used to navigate to find results from a "Find in Files" type of operation.
    [ProvideEditorLogicalView(typeof(EditorFactory), VSConstants.LOGVIEWID.TextView_string)]
    [Guid(GuidList.guidQuantifiedDevVisualStudioExtensionPkgString)]
    [ProvideAutoLoadAttribute("{F1536EF8-92EC-443C-9ED7-FDADF150DA82}")]
    public sealed class QuantifiedDevVisualStudioExtensionPackage : Package
    {
        private bool buildSucceeded;
        private double latitude;
        private double longitude;
        private bool isOn;
        private string context = "QuantifiedDev";

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public QuantifiedDevVisualStudioExtensionPackage()
        {
// ReSharper disable RedundantStringFormatCall
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", ToString()));
// ReSharper restore RedundantStringFormatCall
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = FindToolWindow(typeof(MyToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            var windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }


        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            isOn = true;
            Debug.WriteLine (CultureInfo.CurrentCulture.ToString(), "Entering Initialize() of: {0}", this);
            base.Initialize();

            //Create Editor Factory. Note that the base Package class will call Dispose on it.
            RegisterEditorFactory(new EditorFactory(this));

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                // Create the command for the menu item.
                var menuCommandId = new CommandID(GuidList.guidQuantifiedDevVisualStudioExtensionCmdSet, (int)PkgCmdIDList.cmdidQuantifiedDev);
                var menuItem = new MenuCommand(MenuItemCallback, menuCommandId );
                mcs.AddCommand( menuItem );
                // Create the command for the tool window
                var toolwndCommandId = new CommandID(GuidList.guidQuantifiedDevVisualStudioExtensionCmdSet, (int)PkgCmdIDList.cmdidQuantifiedDevTool);
                var menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandId);
                mcs.AddCommand( menuToolWin );
            }

            InitializeSendingBuildEvents();
        }
        #endregion

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            // Show a Message Box to prove we were here
            var uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            var lat = latitude;
            var @long = longitude;
            ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       "QuantifiedDevVisualStudioExtension",
                       string.Format(CultureInfo.CurrentCulture, "Quantified dev is enabled, your lat/long is {0},{1}. If this is wrong please go to view>other windows>quantified dev and enter the correct location", lat, @long),
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       OLEMSGICON.OLEMSGICON_INFO,
                       0,        // false
                       out result));
        }

        private void InitializeSendingBuildEvents()
        {
            Debug.WriteLine(CultureInfo.CurrentCulture.ToString(), "Entering Initialize() of: {0}", this);
         
            const string context = "SYKES";

            if (Settings.Default.StreamId == "")
            {
                CreateStream();
            }

            GetLatLong();
            

            var dte = (DTE)GetService(typeof(DTE));
            dte.Events.BuildEvents.OnBuildBegin += (scope, action) =>
            {
                buildSucceeded = true;
                SendBuildEvent(context, scope, action, new object[] { "Build", "Start" }, new JObject());
            };

            dte.Events.BuildEvents.OnBuildDone += (scope, action) =>
            {
                var properties = new JObject();
                properties["Result"] = buildSucceeded ? "Success" : "Failure";
                SendBuildEvent(context, scope, action, new object[] { "Build", "Finish" }, properties);
            };

            dte.Events.BuildEvents.OnBuildProjConfigDone += (project, config, platform, solutionConfig, success) =>
            {
                Debug.WriteLine("Project Build Begin", context);
                Debug.WriteLine(project, context);
                Debug.WriteLine(config, context);
                Debug.WriteLine(platform, context);
                Debug.WriteLine(solutionConfig, context);
                Debug.WriteLine(success, context);
                buildSucceeded &= success;  
            };


        }

        private void GetLatLong()
        {
            if (Settings.Default.Latitude != 0 || Settings.Default.Longitude != 0)
            {
                latitude = Settings.Default.Latitude;
                longitude = Settings.Default.Longitude;
                return;
            }
           
            if (isOn == false)
                return;

            HttpClient client = new HttpClient();
            var url = string.Format("http://freegeoip.net/json");

            client.GetAsync(url).ContinueWith(postTask =>
            {
                try
                {
                    HttpResponseMessage result = postTask.Result;
                    if (result.StatusCode != HttpStatusCode.OK)
                    {
                        isOn = false;
                        return;
                    }

                    result.Content.ReadAsStringAsync().ContinueWith(bodyContent =>
                    {
                        if (bodyContent.IsCompleted == false)
                        {
                            return;
                        }

                        var location = JObject.Parse(bodyContent.Result);
                        latitude = double.Parse(location["latitude"].ToString());
                        longitude = double.Parse(location["longitude"].ToString());
                        Settings.Default.Latitude = latitude;
                        Settings.Default.Longitude = longitude;
                        Settings.Default.Save();
                    });
                }
                catch (Exception)
                {
                    WriteToOutput("Couldn't get lat and long automatically");
                    isOn = false;
                }
            });
        }

        private void SendBuildEvent(string context, vsBuildScope scope, vsBuildAction action, object[] actionTags, JObject properties)
        {
            if (isOn == false)
                return;

            var buildActionName = actionTags[0] + " " + actionTags[1];
            Debug.WriteLine(buildActionName, context);

            var streamId = Settings.Default.StreamId;
            var token = Settings.Default.WriteToken;

            var client = new HttpClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
            var buildEvent = new JObject();
            buildEvent["dateTime"] = DateTime.UtcNow.ToString("o");

            var location = new JObject(); 
            location["lat"] = latitude;
            location["long"] = longitude; 
            buildEvent["location"] = location;




            buildEvent["actionTags"] = new JArray(actionTags);
            buildEvent["objectTags"] = new JArray(new object[] { "Computer", "Software" });

            properties["Language"] = "C#";
            properties["Environment"] = "VisualStudio2012";
            buildEvent["properties"] = properties;

            var url = string.Format("http://quantifieddev.herokuapp.com/stream/{0}/event", streamId);
            var content = new StringContent(buildEvent.ToString(Newtonsoft.Json.Formatting.None));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            client.PostAsync(url, content).ContinueWith(postTask =>
            {
                try
                {
                    Debug.WriteLine(postTask.Result.StatusCode.ToString(), context, buildActionName);
                    Debug.WriteLine(scope.ToString(), context, buildActionName);
                    Debug.WriteLine(action.ToString(), context, buildActionName);
                }
                catch (Exception)
                {
                    WriteToOutput("QuantifiedDev: Couldn't send build event");                           
                }
                   
            });
        }

        private void WriteToOutput(string message)
        {
            Debug.WriteLine(message, context);
            Console.WriteLine(message, context);
            IVsOutputWindow outputWindow =
        Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

            Guid guidGeneral = Microsoft.VisualStudio.VSConstants.OutputWindowPaneGuid.GeneralPane_guid;
            IVsOutputWindowPane pane;
            int hr = outputWindow.CreatePane(guidGeneral, "General", 1, 0);
            hr = outputWindow.GetPane(guidGeneral, out pane);
            pane.Activate();
            pane.OutputString(string.Format("QuantifiedDev: {0}\r\n", message));
        }


        private void CreateStream()
        {
            var client = new HttpClient();
            HttpResponseMessage result;
            try
            {
                var request = client.PostAsync("http://quantifieddev.herokuapp.com/stream", new StringContent("{}"));
                result = request.Result;
                
            }
            catch (Exception)
            {
                isOn = false;
                return;
            }

            if (result.StatusCode != HttpStatusCode.OK)
            {
                isOn = false;
                return;
            }

            var streamDetails = result.Content.ReadAsStringAsync().Result;
            var jsonStream = JObject.Parse(streamDetails);
            Settings.Default.StreamId = jsonStream.GetValue("streamid").ToString();
            Settings.Default.WriteToken = jsonStream.GetValue("writeToken").ToString();
            Settings.Default.ReadToken = jsonStream.GetValue("readToken").ToString();
            Settings.Default.Save();
            Debug.WriteLine(streamDetails);
        }

    }
}
