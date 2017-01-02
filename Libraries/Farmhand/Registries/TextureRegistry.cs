﻿namespace Farmhand.Registries
{
    using System.Collections.Generic;

    using Farmhand.Registries.Containers;

    /// <summary>
    ///     Holds a reference to loaded textures. This class stores ordinary textures passed through but it primarily used to
    ///     store mod textures
    /// </summary>
    public static class TextureRegistry
    {
        private static Registry<string, DiskTexture> textureRegistryInstance;

        private static Registry<string, DiskTexture> RegistryInstance
            => textureRegistryInstance ?? (textureRegistryInstance = new Registry<string, DiskTexture>());

        /// <summary>
        ///     Returns all registered textures
        /// </summary>
        /// <returns>
        ///     An <see cref="IEnumerable{DiskTexture}" /> of all registered items.
        /// </returns>
        public static IEnumerable<DiskTexture> GetRegisteredTextures()
        {
            return RegistryInstance.GetRegisteredItems();
        }

        /// <summary>
        ///     Returns item with matching id
        /// </summary>
        /// <param name="itemId">ID of the item to return</param>
        /// <param name="mod">Owning mod, defaults to null</param>
        /// <returns>
        ///     The <see cref="DiskTexture" /> for this ID
        /// </returns>
        public static DiskTexture GetItem(string itemId, ModManifest mod = null)
        {
            return RegistryInstance.GetItem(mod == null ? itemId : GetModSpecificId(mod, itemId));
        }

        /// <summary>
        ///     Registers item with it
        /// </summary>
        /// <param name="itemId">ID of the item to register</param>
        /// <param name="item">Item to register</param>
        /// <param name="mod">Owning mod, defaults to null</param>
        public static void RegisterItem(string itemId, DiskTexture item, ModManifest mod = null)
        {
            RegistryInstance.RegisterItem(mod == null ? itemId : GetModSpecificId(mod, itemId), item);
        }

        /// <summary>
        ///     Removes an item with ID
        /// </summary>
        /// <param name="itemId">ID of the item to remove</param>
        /// <param name="mod">Owning mod, defaults to null</param>
        public static void UnregisterItem(string itemId, ModManifest mod = null)
        {
            RegistryInstance.UnregisterItem(mod == null ? itemId : GetModSpecificId(mod, itemId));
        }

        #region Helper Functions

        private static string GetModSpecificPrefix(ModManifest mod)
        {
            return $"\\{mod.UniqueId.ThisId}\\";
        }

        /// <summary>
        ///     Gets a mod specific ID
        /// </summary>
        /// <param name="mod">
        ///     The owning mod.
        /// </param>
        /// <param name="itemId">
        ///     The ID of the item to get the modified ID for.
        /// </param>
        /// <returns>
        ///     The mod specific ID.
        /// </returns>
        public static string GetModSpecificId(ModManifest mod, string itemId)
        {
            var modPrefix = GetModSpecificPrefix(mod);
            return $"{modPrefix}{itemId}";
        }

        #endregion
    }
}