﻿using Autofac;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenMod.API.Plugins;
using OpenMod.API.Users;
using OpenMod.Core.Ioc;
using OpenMod.Core.Permissions;
using OpenMod.Core.Permissions.Data;
using OpenMod.Unturned.Players;
using OpenMod.Unturned.Plugins;
using OpenMod.Unturned.RocketMod;
using SDG.Unturned;
using System;
using System.Drawing;
using System.Threading.Tasks;

[assembly: PluginMetadata("PermissionExtensions", Author = "EvolutionPlugins", DisplayName = "Permission Extensions",
    Website = "https://discord.gg/6KymqGv")]

namespace PermissionExtensions
{
    public class PermissionExtensions : OpenModUnturnedPlugin
    {
        private readonly IPermissionRolesDataStore m_PermissionRolesDataStore;
        private readonly IUserDataStore m_UserDataStore;
        private readonly ILogger<PermissionExtensions> m_Logger;
        private readonly RocketModHandleChatPatch m_HandleChatPatch = null!;

        public PermissionExtensions(IServiceProvider serviceProvider, IPermissionRolesDataStore permissionRolesDataStore,
            IUserDataStore userDataStore, ILogger<PermissionExtensions> logger, ILifetimeScope lifetimeScope) : base(serviceProvider)
        {
            m_PermissionRolesDataStore = permissionRolesDataStore;
            m_UserDataStore = userDataStore;
            m_Logger = logger;

            if (RocketModIntegration.IsRocketModUnturnedLoaded(out var asm))
            {
                m_HandleChatPatch =
                    ActivatorUtilitiesEx.CreateInstance<RocketModHandleChatPatch>(lifetimeScope, new object[] { m_Logger, Harmony, asm! });
            }
        }

        protected override UniTask OnLoadAsync()
        {
            m_Logger.LogInformation("Made with <3 by EvolutionPlugins");
            m_Logger.LogInformation("https://github.com/evolutionplugins \\ https://github.com/diffoz");
            m_Logger.LogInformation("Support discord: https://discord.gg/6KymqGv");

            return AddExample().AsUniTask(false);
        }

        internal void CallRocketEvent(UnturnedPlayer player, EChatMode chatMode, string message, ref Color color, ref bool cancel)
        {
            if (m_HandleChatPatch == null)
            {
                return;
            }

            m_HandleChatPatch.CallRocketEventInternal(player.Player, chatMode, ref color, message, ref cancel);
        }

        public async Task<PermissionRoleData?> GetOrderedPermissionRoleData(string id, string type)
        {
            PermissionRoleData? result = null;

            var data = await m_UserDataStore.GetUserDataAsync(id, type);
            if (data?.Roles != null)
            {
                foreach (var roleId in data.Roles)
                {
                    var role = await m_PermissionRolesDataStore.GetRoleAsync(roleId);
                    if (role == null)
                    {
                        continue;
                    }

                    if ((result?.Priority ?? int.MinValue) < role.Priority)
                    {
                        result = role;
                    }
                }
            }

            return result;
        }

        private Task AddExample()
        {
            if (!Configuration.GetSection("addExample").Get<bool>())
            {
                return Task.CompletedTask;
            }

            foreach (var role in m_PermissionRolesDataStore.Roles)
            {
                if (role?.Data == null)
                {
                    continue;
                }

                if (!role.Data.ContainsKey("color"))
                {
                    role.Data.Add("color", "white");
                }
                if (!role.Data.ContainsKey("prefix"))
                {
                    role.Data.Add("prefix", string.Empty);
                }
                if (!role.Data.ContainsKey("suffix"))
                {
                    role.Data.Add("suffix", string.Empty);
                }
            }

            return m_PermissionRolesDataStore.SaveChangesAsync();
        }
    }
}
