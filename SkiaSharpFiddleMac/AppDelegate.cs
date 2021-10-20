using System;
using System.IO;
using AppKit;
using Foundation;
using SkiaSharpFiddle.ViewModels;
using SkiaSharpFiddle.XF;
using Xamarin.Forms;
using Xamarin.Forms.Platform.MacOS;

namespace SkiaSharpFiddleMac
{
    [Register("AppDelegate")]
    public class AppDelegate : FormsApplicationDelegate
    {
        NSWindow window;

        public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender) => true;

        public AppDelegate()
        {
            var style = NSWindowStyle.Closable | NSWindowStyle.Resizable | NSWindowStyle.Titled;
            var rect = new CoreGraphics.CGRect(200, 1000, 1024, 768);
            window = new NSWindow(rect, style, NSBackingStore.Buffered, false);
            window.TitleVisibility = NSWindowTitleVisibility.Visible;
            window.Title = "SkiaSharpFiddle";
        }

        public override NSWindow MainWindow
        {
            get { return window; }
        }

        public override void DidFinishLaunching(NSNotification notification)
        {
            Forms.Init();
            LoadApplication(new App());
            base.DidFinishLaunching(notification);
        }

        void OpenFile(NSUrl url)
        {
            var mainWindow = Application.Current.MainPage as MainWindow;
            mainWindow.Editor.Text = new StreamReader(url.Path).ReadToEnd();
            
            var model = Application.Current.MainPage.BindingContext as MainViewModel;
            model.OpenFilePath = url.Path;
        }
        
        [Export ("openDocument:")]
        void OpenDialog (NSObject sender)
        {
            var dlg = NSOpenPanel.OpenPanel;
            dlg.CanChooseFiles = true;
            dlg.CanChooseDirectories = false;

            if (dlg.RunModal() == 1)
            {
                var url = dlg.Urls[0];
                if (url != null)
                {
                    OpenFile(url);
                }
            }
        }

        [Export("saveDocumentAs:")]
        void SaveFileAs(NSObject sender)
        {
            var model = Application.Current.MainPage.BindingContext as MainViewModel;
            var dlg = NSSavePanel.SavePanel;

            if (dlg.RunModal() == 1)
            {
                using (var write = new StreamWriter(dlg.Url.Path))
                {
                    write.Write(model.SourceCode);
                }
            }
        }

        [Export("saveDocument:")]
        void SaveDialog(NSObject sender)
        {
            var model = Application.Current.MainPage.BindingContext as MainViewModel;
            if (!String.IsNullOrEmpty(model.OpenFilePath))
            {
                using (var write = new StreamWriter(model.OpenFilePath))
                {
                    write.Write(model.SourceCode);
                }
            }
            else
            {
                SaveFileAs(sender);
            }
        }
    }
}
