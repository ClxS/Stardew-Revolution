﻿namespace Farmhand.Registries.Containers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Farmhand.Helpers;
    using Farmhand.Logging;

    using Microsoft.Xna.Framework.Graphics;

    using Newtonsoft.Json;

    using xTile;

    /// <summary>
    ///     Contains registration information for a Farmhand mod.
    /// </summary>
    public class ModManifest : IModManifest
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ModManifest" /> class.
        /// </summary>
        public ModManifest()
        {
            this.ModState = ModState.Unloaded;
        }

        /// <summary>
        ///     Gets the mod DLL.
        /// </summary>
        [JsonProperty]
        public string ModDll { get; internal set; }

        /// <summary>
        ///     Gets the mod dependencies.
        /// </summary>
        [JsonProperty]
        public List<ModDependency> Dependencies { get; internal set; }

        /// <summary>
        ///     Gets the content for this mod.
        /// </summary>
        [JsonProperty]
        public ManifestContent Content { get; internal set; }

        #region IModManifest Members

        /// <summary>
        ///     Gets the unique ID for this mod.
        /// </summary>
        [JsonProperty]
        public string UniqueId { get; internal set; }

        /// <summary>
        ///     Gets whether this is a Farmhand mod.
        /// </summary>
        public bool IsFarmhandMod => true;

        /// <summary>
        ///     Gets the name of this mod.
        /// </summary>
        [JsonProperty]
        public string Name { get; internal set; }

        /// <summary>
        ///     Gets the author of this mod.
        /// </summary>
        [JsonProperty]
        public string Author { get; internal set; }

        /// <summary>
        ///     Gets the version of this mod.
        /// </summary>
        [JsonProperty]
        public Version Version { get; internal set; }

        /// <summary>
        ///     Gets the description of this mod.
        /// </summary>
        [JsonProperty]
        public string Description { get; internal set; }

        #endregion

        /// <summary>
        ///     Fires just prior to loading the mod.
        /// </summary>
        public static event EventHandler BeforeLoaded;

        /// <summary>
        ///     Fires just after loading the mod.
        /// </summary>
        public static event EventHandler AfterLoaded;

        internal void OnBeforeLoaded()
        {
            BeforeLoaded?.Invoke(this, EventArgs.Empty);
        }

        internal void OnAfterLoaded()
        {
            AfterLoaded?.Invoke(this, EventArgs.Empty);
        }

        #region Manifest Instance Data

        /// <summary>
        ///     Gets the configuration path for this mod.
        /// </summary>
        [JsonIgnore]
        public string ConfigurationPath => Path.Combine(this.ModDirectory, "Config");

        /// <summary>
        ///     Gets the mod load state.
        /// </summary>
        [JsonIgnore]
        public ModState ModState { get; internal set; }

        /// <summary>
        ///     Gets or sets the last exception thrown by this mod.
        /// </summary>
        [JsonIgnore]
        public Exception LastException { get; set; }

        /// <summary>
        ///     Gets whether this mod has a DLL.
        /// </summary>
        [JsonIgnore]
        public bool HasDll => !string.IsNullOrWhiteSpace(this.ModDll);

        /// <summary>
        ///     Gets whether this mod has a config file.
        /// </summary>
        [JsonIgnore]
        public bool HasConfig => this.HasDll && !string.IsNullOrWhiteSpace(this.ConfigurationPath);

        /// <summary>
        ///     Gets whether this mod has defined content.
        /// </summary>
        [JsonIgnore]
        public bool HasContent => this.Content != null && this.Content.HasContent;

        /// <summary>
        ///     Gets the mod assembly.
        /// </summary>
        [JsonIgnore]
        public Assembly ModAssembly { get; internal set; }

        /// <summary>
        ///     Gets the instance.
        /// </summary>
        [JsonIgnore]
        public Mod Instance { get; internal set; }

        /// <summary>
        ///     Gets the mod directory.
        /// </summary>
        [JsonIgnore]
        public string ModDirectory { get; internal set; }

        internal bool LoadModDll()
        {
            if (this.Instance != null)
            {
                throw new Exception("Error! Mod has already been loaded!");
            }

            try
            {
                var modDllPath = Path.Combine(this.ModDirectory, this.ModDll);
                if (!modDllPath.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase))
                {
                    modDllPath += ".dll";
                }

                this.ModAssembly = Assembly.LoadFile(modDllPath);
                if (this.ModAssembly.GetTypes().Count(x => x.BaseType == typeof(Mod)) > 0)
                {
                    var type = this.ModAssembly.GetTypes().First(x => x.BaseType == typeof(Mod));
                    var instance = (Mod)this.ModAssembly.CreateInstance(type.ToString());
                    if (instance != null)
                    {
                        instance.ModSettings = this;
                        if (instance.ConfigurationSettings != null)
                        {
                            var config = instance.ConfigurationSettings;
                            config.Manifest = this;
                            if (config.DoesConfigurationFileExist())
                            {
                                config.Load();
                            }
                            else
                            {
                                // If there isn't an already saved config file to load, we should create one.
                                config.Save();
                            }
                        }

                        instance.Entry();
                    }

                    this.Instance = instance;
                    Log.Verbose($"Loaded mod dll: {this.Name}");
                }

                if (this.Instance == null)
                {
                    throw new Exception("Invalid Mod DLL");
                }
            }
            catch (Exception ex)
            {
                var exception = ex as ReflectionTypeLoadException;
                if (exception != null)
                {
                    foreach (var e in exception.LoaderExceptions)
                    {
                        Log.Exception(
                            "LoaderExceptions entry: "
                            + $"{e.Message} ${e.Source} ${e.TargetSite} ${e.StackTrace} ${e.Data}",
                            e);
                    }
                }

                Log.Exception("Error loading mod DLL", ex);
            }

            return this.Instance != null;
        }

        internal void LoadContent()
        {
            if (this.Content == null)
            {
                return;
            }

            Log.Verbose("Loading Content");
            if (this.Content.Textures != null)
            {
                foreach (var texture in this.Content.Textures)
                {
                    texture.AbsoluteFilePath = Path.Combine(
                        this.ModDirectory,
                        Constants.ModContentDirectory,
                        texture.File);

                    if (!texture.Exists())
                    {
                        throw new Exception($"Missing Texture: {texture.AbsoluteFilePath}");
                    }

                    Log.Verbose($"Registering new texture: {texture.Id}");
                    TextureRegistry.RegisterItem(texture.Id, texture, this);
                }
            }

            if (this.Content.Maps != null)
            {
                foreach (var map in this.Content.Maps)
                {
                    map.AbsoluteFilePath = Path.Combine(this.ModDirectory, Constants.ModContentDirectory, map.File);

                    if (!map.Exists())
                    {
                        throw new Exception($"Missing map: {map.AbsoluteFilePath}");
                    }

                    Log.Verbose($"Registering new map: {map.Id}");
                    MapRegistry.RegisterItem(map.Id, map, this);
                }
            }

            if (this.Content.Xnb == null)
            {
                return;
            }

            foreach (var file in this.Content.Xnb)
            {
                if (file.IsXnb)
                {
                    file.AbsoluteFilePath = Path.Combine(this.ModDirectory, Constants.ModContentDirectory, file.File);
                }

                file.OwningMod = this;
                if (!file.Exists(this))
                {
                    if (file.IsXnb)
                    {
                        throw new Exception($"Replacement File: {file.AbsoluteFilePath}");
                    }

                    if (file.IsTexture)
                    {
                        throw new Exception($"Replacement Texture: {file.Texture}");
                    }
                }

                Log.Verbose("Registering new texture XNB override");
                XnbRegistry.RegisterItem(file.Original, file, this);
            }
        }

        /// <summary>
        ///     Gets a texture registered by this mod via it's manifest file
        /// </summary>
        /// <param name="id">
        ///     The id of the texture.
        /// </param>
        /// <returns>
        ///     The registered <see cref="Texture2D" />.
        /// </returns>
        /// <exception cref="NullReferenceException">
        ///     Thrown if no matching texture was found in <see cref="TextureRegistry" />.
        /// </exception>
        public Texture2D GetTexture(string id)
        {
            var item = TextureRegistry.GetItem(id, this);
            if (item == null)
            {
                throw new NullReferenceException($"Found no matching registered texture in TextureRegistry: {id}");
            }

            return item.Texture;
        }

        /// <summary>
        ///     Gets a map registered by this mod via it's manifest file
        /// </summary>
        /// <param name="id">
        ///     The id of the map.
        /// </param>
        /// <returns>
        ///     The registered <see cref="Map" />.
        /// </returns>
        /// <exception cref="NullReferenceException">
        ///     Thrown if no matching texture was found in <see cref="MapRegistry" />.
        /// </exception>
        public Map GetMap(string id)
        {
            var item = MapRegistry.GetItem(id, this);
            if (item == null)
            {
                throw new NullReferenceException($"Found no matching registered map in MapRegistry: {id}");
            }

            return item.Map;
        }

        #endregion
    }
}