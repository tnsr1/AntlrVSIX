﻿using Options;

namespace LspAntlr
{
    using LanguageServer;
    using Microsoft.VisualStudio.LanguageServer.Client;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Threading;
    using Microsoft.VisualStudio.Utilities;
    using StreamJsonRpc;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Task = System.Threading.Tasks.Task;

    [ContentType("Antlr")]
    [Export(typeof(ILanguageClient))]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(AntlrLanguageClient.PackageGuidString)]
    public class AntlrLanguageClient : AsyncPackage, ILanguageClient, ILanguageClientCustomMessage2
    {
        public const string PackageGuidString = "49bf9144-398a-467c-9b87-ac26d1e62737";
        public static MemoryStream _log_from_server = new MemoryStream();
        public static MemoryStream _log_to_server = new MemoryStream();
        private JsonRpc _rpc;

        public AntlrLanguageClient()
        {
            Instance = this;
            OptionsCommand.Initialize(this);
            AboutCommand.Initialize(this);
        }

        public event AsyncEventHandler<EventArgs> StartAsync;

        public event AsyncEventHandler<EventArgs> StopAsync;

        public static AntlrLanguageClient Instance { get; set; }
        public IEnumerable<string> ConfigurationSections => null;
        public object CustomMessageTarget => null;
        public IEnumerable<string> FilesToWatch => null;
        public object InitializationOptions => null;
        public object MiddleLayer => null;
        public string Name => "Antlr language extension";
        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            await Task.Yield();
            try
            {
                string cache_location = System.IO.Path.GetTempPath();
                var t = typeof(AntlrLanguageClient);
                var a = t.Assembly;
                var f = System.IO.Path.GetFullPath(a.Location);
                var p = System.IO.Path.GetDirectoryName(f);
                var antlr_executable = p + System.IO.Path.DirectorySeparatorChar
                                         + @"Server\net472\LanguageServer.Exec.exe";
                //var w2 = new SetWorkspace(cache_location, antlr_executable);
                //w2.ShowDialog();
                //w2.Close();
                //var workspace_path = w2.workspace_path.Text;
                var workspace_path = cache_location;

                if (workspace_path == null || workspace_path == "")
                    workspace_path = cache_location;

                //antlr_executable = w2.lspserver.Text;

                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = antlr_executable;
                info.WorkingDirectory = workspace_path;
                info.Arguments = workspace_path;
                info.RedirectStandardInput = true;
                info.RedirectStandardOutput = true;
                info.UseShellExecute = false;
                info.CreateNoWindow = ! POptions.GetBoolean("VisibleServerWindow");
                Process process = new Process();
                process.StartInfo = info;
                if (process.Start())
                {
                    bool debug = false;
                    var @out = process.StandardOutput.BaseStream;
                    var eout = debug
                        ? new LspTools.LspHelpers.EchoStream(@out, _log_from_server,
                            LspTools.LspHelpers.EchoStream.StreamOwnership.OwnNone)
                        : @out;
                    var @in = process.StandardInput.BaseStream;
                    var ein = debug
                        ? new LspTools.LspHelpers.EchoStream(@in, _log_to_server,
                            LspTools.LspHelpers.EchoStream.StreamOwnership.OwnNone)
                        : @in;

                    return new Connection(eout, ein);
                }
            }
            catch (Exception e)
            {
            }

            return null;
        }

        public async Task AttachForCustomMessageAsync(JsonRpc rpc)
        {
            await Task.Yield();
            this._rpc = rpc;
        }

        public async Task OnLoadedAsync()
        {
            await StartAsync.InvokeAsync(this, EventArgs.Empty);
        }

        public Task OnServerInitializedAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnServerInitializeFailedAsync(Exception e)
        {
            return Task.CompletedTask;
        }

        public object[] SendServerCustomMessage(int start, int end, string ffn)
        {
            try
            {
                if (this._rpc == null) return null;
                DocumentSymbolSpansParams p = new DocumentSymbolSpansParams();
                var uri = new Uri(ffn);
                p.TextDocument = uri;
                p.Start = start;
                p.End = end;
                var result = this._rpc.InvokeAsync<object[]>("KenCustomMessage", p).Result;
                return result;
            }
            catch (Exception)
            {
            }
            return null;
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            //await Command1.InitializeAsync(this);
        }
    }
}
