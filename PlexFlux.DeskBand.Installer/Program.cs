using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace PlexFlux.DeskBand.Installer
{
    class Program
    {
        [ComImport, Guid("6D67E846-5B9C-4db8-9CBC-DDE12F4254F1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ITrayDeskband
        {
            [PreserveSig]
            int ShowDeskBand([In, MarshalAs(UnmanagedType.Struct)] ref Guid clsid);
            [PreserveSig]
            int HideDeskBand([In, MarshalAs(UnmanagedType.Struct)] ref Guid clsid);
            [PreserveSig]
            int IsDeskBandShown([In, MarshalAs(UnmanagedType.Struct)] ref Guid clsid);
            [PreserveSig]
            int DeskBandRegistrationChanged();
        }

        static void Main(string[] args)
        {
            Console.WriteLine("PlexFlux.DeskBand Installer");

            if (args.Length == 1)
            {
                bool succeed = false;

                if (Environment.Is64BitOperatingSystem && IntPtr.Size != 8)
                {
                    // use 64bit installer pls
                    Console.WriteLine("ERROR: You must use 64bit installer for 64bit OS.");
                }
                else
                {
                    switch (args[0])
                    {
                        case "-install":
                            succeed = Install();
                            break;

                        case "-uninstall":
                            succeed = Uninstall();
                            break;
                    }
                }

                Environment.Exit(succeed ? 0 : 128);
                return;
            }

            ShowHelpMessage();
        }

        private static void ShowHelpMessage()
        {
            // help message
            Console.WriteLine("Usage: PlexFlux.DeskBand.exe options");
            Console.WriteLine("Options:");
            Console.WriteLine(" -install\tRegister this assembly");
            Console.WriteLine(" -uninstall\tUnregister this assembly");
        }

        private static bool Install()
        {
            var regAsm = new RegistrationServices();

            try
            {
                var assembly = LoadAssembly();

                if (regAsm.RegisterAssembly(assembly, AssemblyRegistrationFlags.SetCodeBase))
                {
                    RestartExplorer();

                    // add the deskband to taskbar
                    ITrayDeskband obj = null;
                    Type trayDeskbandType = Type.GetTypeFromCLSID(new Guid("E6442437-6C68-4f52-94DD-2CFED267EFB9"));
                    try
                    {
                        obj = (ITrayDeskband)Activator.CreateInstance(trayDeskbandType);
                        Guid deskbandGuid = new Guid("ADAE1E11-F046-4726-9B7B-F1B78B183877");
                        obj.DeskBandRegistrationChanged();
                        var hr = obj.ShowDeskBand(ref deskbandGuid);
                        if (hr != 0)
                            throw new Exception("Error while trying to show deskband: " + hr);
                        obj.DeskBandRegistrationChanged();
                    }
                    catch
                    {
                    }
                    finally
                    {
                        if (obj != null && Marshal.IsComObject(obj))
                            Marshal.ReleaseComObject(obj);
                    }

                    Console.WriteLine("SUCCESS: DeskBand installed successfully.");
                    return true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("ERROR: You must run this as an administrator.");
            }

            return false;
        }

        private static bool Uninstall()
        {
            var regAsm = new RegistrationServices();

            try
            {
                var assembly = LoadAssembly();

                if (regAsm.UnregisterAssembly(assembly))
                {
                    RestartExplorer();

                    Console.WriteLine("SUCCESS: DeskBand uninstalled successfully.");
                    return true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("ERROR: You must run this as an administrator.");
            }

            return false;
        }

        private static void RestartExplorer()
        {
            Console.WriteLine("Restarting Windows Explorer...");

            foreach (var process in Process.GetProcessesByName("explorer"))
                process.Kill();

            Thread.Sleep(2000);

            // check if explorer has been restarted by the system
            if (Process.GetProcessesByName("explorer").Length == 0)
                Process.Start("explorer.exe");
        }

        private static Assembly LoadAssembly()
        {
            return Assembly.LoadFile(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "PlexFlux.DeskBand.dll"));
        }
    }
}
