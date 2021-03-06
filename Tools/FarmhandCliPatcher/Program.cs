﻿namespace Farmhand.Patcher
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Farmhand.Installers.Patcher;

    internal class Program
    {
        internal static void Main(string[] args)
        {
            Patcher patcher;
            var grmDisabled = args.Any(a => a.Equals("-disablegrm"));
            var path = args.LastOrDefault();
            if (path == null)
            {
                throw new ArgumentNullException(
                    "Required argument (path) was missing. This should be the final argument "
                    + "in the command, and point to the platform staging folder for pass1, or to the output exe for pass2.");
            }

            var noObsolete = args.Any(a => a.Equals("-noobsolete"));

            if (args.Any(a => a.Equals("-pass1")))
            {
                path = Path.Combine(path, path.EndsWith("Windows") ? "Stardew Valley.exe" : "StardewValley.exe");
                patcher = CreatePatcher(Pass.PassOne);

                PatcherOptions.DisableGrm = grmDisabled;
                PatcherOptions.NoObsolete = noObsolete;
                if (noObsolete)
                {
                    PatcherOptions.OutputOverride = PatcherConstants.PassOneFarmhandExeNoObsolete;
                }

                patcher.PatchStardew(path);
            }
            else if (args.Any(a => a.Equals("-pass2")))
            {
                patcher = CreatePatcher(Pass.PassTwo);
                PatcherOptions.DisableGrm = grmDisabled;
                PatcherOptions.NoObsolete = noObsolete;
                if (noObsolete)
                {
                    PatcherOptions.OutputOverride = "Stardew Farmhand No-Obsolete.exe";
                }

                patcher.PatchStardew(path);
            }
            else
            {
                Console.WriteLine("Invalid build pass");
            }
        }

        internal static Patcher CreatePatcher(Pass pass)
        {
            var patcherAssembly =
                Assembly.LoadFrom(
                    pass == Pass.PassOne ? "FarmhandPatcherFirstPass.dll" : "FarmhandPatcherSecondPass.dll");
            var patcherType =
                patcherAssembly.GetType(
                    pass == Pass.PassOne
                        ? "Farmhand.Installers.Patcher.PatcherFirstPass"
                        : "Farmhand.Installers.Patcher.PatcherSecondPass");
            var patcher = Activator.CreateInstance(patcherType);
            return patcher as Patcher;
        }

        public enum Pass
        {
            /// <summary>
            ///     First Pass
            /// </summary>
            PassOne,

            /// <summary>
            ///     Second Pass
            /// </summary>
            PassTwo
        }
    }
}