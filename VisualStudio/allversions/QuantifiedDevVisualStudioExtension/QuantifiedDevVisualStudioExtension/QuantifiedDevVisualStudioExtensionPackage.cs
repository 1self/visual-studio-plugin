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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace N1self.C1selfVisualStudioExtension
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
    [ProvideEditorExtension(typeof(EditorFactory), ".1selfvisualstudioextension", 50, 
              ProjectGuid = "{A2FE74E1-B743-11d0-AE1A-00A0C90FFFC3}", 
              TemplateDir = "Templates", 
              NameResourceID = 105,
              DefaultName = "1selfVisualStudioExtension")]
    [ProvideKeyBindingTable(GuidList.guid1selfVisualStudioExtensionEditorFactoryString, 102)]
    // Our Editor supports Find and Replace therefore we need to declare support for LOGVIEWID_TextView.
    // This attribute declares that your EditorPane class implements IVsCodeWindow interface
    // used to navigate to find results from a "Find in Files" type of operation.
    [ProvideEditorLogicalView(typeof(EditorFactory), VSConstants.LOGVIEWID.TextView_string)]
    [Guid(GuidList.guid1selfVisualStudioExtensionPkgString)]
    [ProvideAutoLoadAttribute("{F1536EF8-92EC-443C-9ED7-FDADF150DA82}")]
    public sealed class C1selfVisualStudioExtensionPackage : Package
    {
        private bool buildSucceeded;
        private double latitude;
        private double longitude;
        private bool isOn;
        private string context = "1self";
        private BuildEvents buildEvents;
        private TextEditorEvents textEditorEvents;
        private DebuggerEvents debuggerEvents;
        private DocumentEvents documentEvents;
        private FindEvents findEvents;
        private ProjectItemsEvents miscFilesEvents;
        private SelectionEvents selectionEvents;
        private SolutionEvents solutionEvents;
        private ProjectItemsEvents solutionItemsEvents;
        private WindowEvents windowEvents;
        private ConcurrentQueue<Tuple<DateTime, string>> activityQueue = new ConcurrentQueue<Tuple<DateTime,string>>();
        private List<Tuple<DateTime,string>> currentWindow = new List<Tuple<DateTime, string>>();
        private System.Threading.Timer timer;
        private DateTime buildStartTime;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public C1selfVisualStudioExtensionPackage()
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
                var menuCommandId = new CommandID(GuidList.guid1selfVisualStudioExtensionCmdSet, (int)PkgCmdIDList.cmdid1self);
                var menuItem = new MenuCommand(MenuItemCallback, menuCommandId );
                mcs.AddCommand( menuItem );
                // Create the command for the tool window
                var toolwndCommandId = new CommandID(GuidList.guid1selfVisualStudioExtensionCmdSet, (int)PkgCmdIDList.cmdid1selfTool);
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
                       "1selfVisualStudioExtension",
                       string.Format(CultureInfo.CurrentCulture, "1self is enabled, your lat/long is {0},{1}. If this is wrong please go to view>other windows>1self and enter the correct location", lat, @long),
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       OLEMSGICON.OLEMSGICON_INFO,
                       0,        // false
                       out result));
        }

        private void SendActivityEvent()
        {
            if (isOn == false)
                return;

            if(currentWindow.Count == 0)
            {
                return;
            }
            var startTime = currentWindow[0].Item1;
            var endTime = currentWindow[currentWindow.Count - 1].Item1;

            currentWindow.Clear();

            var streamId = Settings.Default.StreamId;
            var token = Settings.Default.WriteToken;

            var client = new HttpClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
            var activityEvent = new JObject();
            activityEvent["dateTime"] = DateTime.Now.ToString("o");

            var location = new JObject();
            location["lat"] = latitude;
            location["long"] = longitude;
            activityEvent["location"] = location;

            activityEvent["actionTags"] = new JArray(new object[] {"Develop"});
            activityEvent["objectTags"] = new JArray(new object[] { "Computer", "Software" });

            JObject properties = new JObject();
            properties["Language"] = "C#";
            properties["Environment"] = "VisualStudio";
            properties["duration"] = (endTime - startTime).TotalSeconds;
            activityEvent["properties"] = properties;

            var url = string.Format("https://api.1self.co/v1/streams/{0}/events", streamId);
            var content = new StringContent(activityEvent.ToString(Newtonsoft.Json.Formatting.None));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            Debug.WriteLine(activityEvent.ToString());

            client.PostAsync(url, content).ContinueWith(postTask =>
            {
                try
                {
                    Debug.WriteLine(postTask.Result.StatusCode.ToString(), context);
                }
                catch (Exception)
                {
                    WriteToOutput("1self: Couldn't send build event");
                }

            });
        }

        private void InitializeSendingBuildEvents()
        {
            Debug.WriteLine(CultureInfo.CurrentCulture.ToString(), "Entering Initialize() of: {0}", this);
         
            const string context = "1SELF";

            if (Settings.Default.StreamId == "")
            {
                CreateStream();
            }

            GetLatLong();
            GetInformationMessage();

            var timerDuration1minutes = 60 * 1000;

            System.Threading.AutoResetEvent autoEvent = new System.Threading.AutoResetEvent(false);
            timer = new System.Threading.Timer((state) =>
            {   
                Tuple<DateTime, string> outEvent;
                var newEvents = false;
                while(activityQueue.TryDequeue(out outEvent)){
                    currentWindow.Add(outEvent);
                    newEvents = true;
                }

                if (newEvents)
                {
                    var startTime = currentWindow[0].Item1;
                    var endTime = currentWindow[currentWindow.Count - 1].Item1;
                    if (endTime - startTime > TimeSpan.FromMinutes(20))
                    {
                        SendActivityEvent();
                    }
                }
                else
                {
                    SendActivityEvent();
                }

                timer.Change(timerDuration1minutes, System.Threading.Timeout.Infinite);
            },
            autoEvent,
            timerDuration1minutes,
            System.Threading.Timeout.Infinite);
            

            var dte = (DTE)GetService(typeof(DTE));
            buildEvents = dte.Events.BuildEvents;
            debuggerEvents = dte.Events.DebuggerEvents;
            documentEvents = dte.Events.DocumentEvents;
            findEvents = dte.Events.FindEvents;
            miscFilesEvents = dte.Events.MiscFilesEvents;
            selectionEvents = dte.Events.SelectionEvents;
            solutionEvents = dte.Events.SolutionEvents;
            solutionItemsEvents = dte.Events.SolutionItemsEvents;
            windowEvents = dte.Events.WindowEvents;
            textEditorEvents = dte.Events.TextEditorEvents;

            buildEvents.OnBuildBegin += (scope, action) =>
            {
                buildSucceeded = true;
                buildStartTime = DateTime.Now;
                SendBuildEvent(context, scope, action, new object[] { "Build", "Start" }, new JObject());
            };

            buildEvents.OnBuildDone += (scope, action) =>
            {
                Console.WriteLine(CultureInfo.CurrentCulture.ToString(), "OnBuildDone  ");
         
                var properties = new JObject();
                properties["Result"] = buildSucceeded ? "Success" : "Failure";
                properties["duration"] = (DateTime.Now - buildStartTime).TotalSeconds;
                SendBuildEvent(context, scope, action, new object[] { "Build", "Finish" }, properties);
            };

            buildEvents.OnBuildProjConfigDone += (project, config, platform, solutionConfig, success) =>
            {
                Debug.WriteLine("Project Build Begin", context);
                Debug.WriteLine(project, context);
                Debug.WriteLine(config, context);
                Debug.WriteLine(platform, context);
                Debug.WriteLine(solutionConfig, context);
                Debug.WriteLine(success, context);
                buildSucceeded &= success;  
            };

            documentEvents.DocumentClosing += (Document doc) =>
            {
                trackActivity("doc closing");
            };

            documentEvents.DocumentOpened += (Document doc) =>
            {
                trackActivity("doc opened");
            };

            documentEvents.DocumentOpening += (string DocumentPath, bool ReadOnly) =>
            {
                trackActivity("doc opening");
            };

            documentEvents.DocumentSaved += (Document doc) =>
            {
                trackActivity("doc saved");
            };

            debuggerEvents.OnContextChanged += (EnvDTE.Process NewProcess, Program NewProgram, Thread NewThread, EnvDTE.StackFrame NewStackFrame) =>
            {
                trackActivity("debugger context changed");
            };

            debuggerEvents.OnEnterBreakMode += (dbgEventReason Reason, ref dbgExecutionAction ExecutionAction) =>
            {
                trackActivity("enter break mode");
            };

            debuggerEvents.OnEnterRunMode += (dbgEventReason Reason) =>
            {
                trackActivity("enter run mode");
            };

            debuggerEvents.OnExceptionNotHandled += (string ExceptionType, string Name, int Code, string Description, ref dbgExceptionAction ExceptionAction) =>
            {
                trackActivity("exception not handled");
            };

            debuggerEvents.OnExceptionThrown += (string ExceptionType, string Name, int Code, string Description, ref dbgExceptionAction ExceptionAction) =>
            {
                trackActivity("exception thrown");
            };

            findEvents.FindDone += (vsFindResult Result, bool Cancelled) =>
            {
 	            trackActivity("find done");
            };

            miscFilesEvents.ItemAdded += (ProjectItem ProjectItem) =>
            {
 	            trackActivity("item added");
            }; 
            
            
            miscFilesEvents.ItemRemoved += (ProjectItem ProjectItem) =>
            {
 	            trackActivity("item removed");
            };
            
            miscFilesEvents.ItemRenamed += (ProjectItem projectItem, string oldName) =>
            {
 	            trackActivity("item renamed");
            };
            
            selectionEvents.OnChange += () =>
            {
                trackActivity("selection change");
            };
            
            solutionEvents.Opened += () =>
            {
 	            trackActivity("solution opened");
            };
            
            solutionEvents.ProjectAdded += (Project Project) =>
            {
 	            trackActivity("project added");
            };

            solutionEvents.ProjectRemoved += (Project Project) =>
            {
 	            trackActivity("project removed");
            };
            
            solutionEvents.ProjectRenamed += (Project project, string oldname) =>
            {
 	            trackActivity("project renamed");
            };

            solutionEvents.Renamed += (string oldname) =>
            {
 	            trackActivity("solution renamed");
            };
            
            solutionItemsEvents.ItemAdded += (ProjectItem ProjectItem) =>
            {
 	            trackActivity("solution item added");
            };
            
            solutionItemsEvents.ItemRemoved += (ProjectItem ProjectItem) =>
            {
 	            trackActivity("solution item removed");
            };

            windowEvents.WindowActivated += (Window GotFocus, Window LostFocus) =>
            {
                trackActivity("window activated");
            };

            textEditorEvents.LineChanged += (TextPoint StartPoint, TextPoint EndPoint, int Hint) =>
            {
                trackActivity("Line Changed");
            };
            
            //dte.Events.SolutionItemsEvents.ItemRenamed += (ProjectItem ProjectItem) =>
            //{
            //    trackActivity("solution item renamed");
            //};   
        }

        void trackActivity(string source)
        {
            var thisEvent = new Tuple<DateTime, string>(DateTime.Now, source);
            activityQueue.Enqueue(thisEvent);
            Debug.WriteLine("Activity: " + thisEvent.Item1 + ": " + thisEvent.Item2);
        }

        private void GetInformationMessage()
        {
            var client = new HttpClient();
            client.GetAsync("http://api.1self.co/quantifieddev/extensions/message").ContinueWith(
                getTask =>
                    {
                        try
                        {
                            HttpResponseMessage response = getTask.Result;
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                var rawContent = response.Content.ReadAsStringAsync().Result;
                                var content = JObject.Parse(rawContent);
                                Settings.Default.InfoText = content["text"].ToString();
                                Settings.Default.Save();

                            }
                        }
                        catch (Exception e)
                        {
                            WriteToOutput("Couldn't get the information message.");
                        }
                    });
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
            properties["Environment"] = "VisualStudio";
            buildEvent["properties"] = properties;

            var url = string.Format("https://api.1self.co/v1/streams/{0}/events", streamId);
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
                    WriteToOutput("1self: Couldn't send build event");                           
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
            pane.OutputString(string.Format("1self: {0}\r\n", message));
        }


        private void CreateStream()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "app-id-1ed9e3e621a063ec8679a885b4e1ec4b:app-secret-3e654db1eeb607bfdf26a0372cf043d27cca985c6a3598519820b52f5eb211b7");
            HttpResponseMessage result;
            try
            {
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri("https://api.1self.co/v1/streams"),
                    Method = HttpMethod.Post,
                    Content = new StringContent("{}")
                };

                var response = client.SendAsync(request);
                result = response.Result;
                
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
