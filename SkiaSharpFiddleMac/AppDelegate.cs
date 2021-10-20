﻿using System;
using AppKit;
using Foundation;
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
            
            NSMenu menubar = new NSMenu();
            NSApplication.SharedApplication.MainMenu = menubar;
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
    }
}