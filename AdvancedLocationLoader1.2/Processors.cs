﻿using System;
using System.IO;

using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI;

using StardewValley;
using Warp = StardewValley.Warp;

using Entoarox.Framework;
using Entoarox.Framework.Extensions;

using Entoarox.AdvancedLocationLoader.Configs;

namespace Entoarox.AdvancedLocationLoader
{
    internal static class Processors
    {
        internal static IContentRegistry ContentRegistry = EntoFramework.GetContentRegistry();
        internal static void ApplyTile(Tile tile)
        {
            int stage = 0;
            int branch = 0;
            try
            {
                stage++; // 1
                if (!string.IsNullOrEmpty(tile.Conditions) && !Conditions.CheckConditionList(tile.Conditions, AdvancedLocationLoaderMod.ConditionResolver))
                {
                    AdvancedLocationLoaderMod.Logger.Log(tile.ToString() +" ~> false", LogLevel.Trace);
                    return;
                }
                stage++; // 2
                GameLocation loc = Game1.getLocationFromName(tile.MapName);
                if(loc==null)
                {
                    AdvancedLocationLoaderMod.Logger.Log("Unable to set required tile, location does not exist: " + tile.ToString(), LogLevel.Error);
                }
                stage++; // 3
                if (tile.TileIndex != null)
                {
                    branch = 1;
                    if (tile.TileIndex < 0)
                    {
                        if (!loc.TryRemoveTile(tile.TileX, tile.TileY, tile.LayerId) && !tile.Optional)
                        {
                            AdvancedLocationLoaderMod.Logger.Log("Unable to remove required tile, tile does not exist: " + tile.ToString(), LogLevel.Error);
                            return;
                        }
                    }
                    else
                        try
                        {
                            loc.SetTile(tile.TileX, tile.TileY, tile.LayerId, (int)tile.TileIndex, tile.SheetId);
                        }
                        catch (Exception err)
                        {
                            if (!tile.Optional)
                                AdvancedLocationLoaderMod.Logger.Log(LogLevel.Error, "Unable to set required tile: " + tile.ToString(), err);
                            return;
                        }
                }
                else
                {
                    branch = 2;
                    try
                    {
                        loc.SetTile(tile.TileX, tile.TileY, tile.LayerId, tile.TileIndexes, (int)tile.Interval, tile.SheetId);
                    }
                    catch (Exception err)
                    {
                        if (!tile.Optional)
                            AdvancedLocationLoaderMod.Logger.Log(LogLevel.Error, "Unable to set required tile: " + tile.ToString(), err);
                        return;
                    }
                }
                stage++; // 4
                AdvancedLocationLoaderMod.Logger.Log(tile.ToString() + " ~> true", LogLevel.Trace);
            }
            catch (Exception err)
            {
                AdvancedLocationLoaderMod.Logger.ExitGameImmediately($"Unable to patch tile, a unexpected error occured at stage {stage}{(branch > 0 ? ("-" + branch) : "")}: {tile}", err);
            }
        }
        internal static void ApplyProperty(Property property)
        {
            try
            {
                if (string.IsNullOrEmpty(property.Conditions) || Conditions.CheckConditionList(property.Conditions, AdvancedLocationLoaderMod.ConditionResolver))
                {
                    AdvancedLocationLoaderMod.Logger.Log(property.ToString()+" ~> true", LogLevel.Trace);
                    if (Game1.getLocationFromName(property.MapName).HasTile(property.TileX, property.TileY, property.LayerId))
                        Game1.getLocationFromName(property.MapName).SetTileProperty(property.TileX, property.TileY, property.LayerId, property.Key, property.Value);
                    else if (property.Optional)
                        return;
                    else
                        AdvancedLocationLoaderMod.Logger.Log("Unable to patch required property, tile does not exist: " + property.ToString(), LogLevel.Error);
                }
                else
                    AdvancedLocationLoaderMod.Logger.Log(property.ToString() + " ~> false", LogLevel.Trace);
            }
            catch (Exception err)
            {
                if(!property.Optional)
                    AdvancedLocationLoaderMod.Logger.ExitGameImmediately("Unable to patch property, a unexpected error occured: " + property.ToString(), err);
            }
        }
        internal static void ApplyWarp(Configs.Warp warp)
        {
            try
            {
                if (!string.IsNullOrEmpty(warp.Conditions) && !Conditions.CheckConditionList(warp.Conditions, AdvancedLocationLoaderMod.ConditionResolver))
                {
                    AdvancedLocationLoaderMod.Logger.Log(warp.ToString() +"~> false", LogLevel.Trace);
                    return;
                }
                Warp _warp = new Warp(warp.TileX, warp.TileY, warp.TargetName, warp.TargetX, warp.TargetY, false);
                GameLocation loc = Game1.getLocationFromName(warp.MapName);
                loc.warps.RemoveAll((a) => a.X == _warp.X && a.Y == _warp.Y);
                loc.warps.Add(_warp);
                AdvancedLocationLoaderMod.Logger.Log(warp.ToString() + "~> true", LogLevel.Trace);
            }
            catch (Exception err)
            {
                AdvancedLocationLoaderMod.Logger.ExitGameImmediately("Unable to patch warp, a unexpected error occured: " + warp.ToString(),err);
            }
        }
        internal static void ApplyTilesheet(Tilesheet tilesheet)
        {
            int stage = 0;
            int branch = 0;
            int skip = 0;
            try
            {
                stage++; // 1
                GameLocation location = Game1.getLocationFromName(tilesheet.MapName);
                stage++; // 2
                if (tilesheet.FileName == null)
                    skip = 1;
                else
                {
                    skip = 2;
                    string fakepath = Path.Combine(Path.GetDirectoryName(tilesheet.FileName), "all_sheet_paths_objects", tilesheet.SheetId, Path.GetFileName(tilesheet.FileName));
                    if (tilesheet.Seasonal)
                        fakepath = fakepath.Replace("all_sheet_paths_objects", Path.Combine("all_sheet_paths_objects", Game1.currentSeason));
                    stage++; // 3
                    ContentRegistry.RegisterXnb(fakepath, tilesheet.Seasonal ? (tilesheet.FileName + "_" + Game1.currentSeason) : tilesheet.FileName);
                    stage++; // 4
                    if (location.map.GetTileSheet(tilesheet.SheetId) != null)
                    {
                        branch = 1;
                        location.map.GetTileSheet(tilesheet.SheetId).ImageSource = fakepath;
                        AdvancedLocationLoaderMod.Logger.Log($"{tilesheet} ~> {(tilesheet.Seasonal ? "seasonal" : "normal")}/override", LogLevel.Trace);
                    }
                    else
                    {
                        branch = 2;
                        Texture2D sheet = Game1.content.Load<Texture2D>(fakepath);
                        location.map.AddTileSheet(new xTile.Tiles.TileSheet(tilesheet.SheetId, location.map, fakepath, new xTile.Dimensions.Size((int)Math.Ceiling(sheet.Width / 16.0), (int)Math.Ceiling(sheet.Height / 16.0)), new xTile.Dimensions.Size(16, 16)));
                        AdvancedLocationLoaderMod.Logger.Log($"{tilesheet} ~> {(tilesheet.Seasonal ? "seasonal" : "normal")}/insert", LogLevel.Trace);
                    }
                }
                stage++; // 5 (skip 3)
                if (tilesheet.Properties.Count > 0)
                {
                    xTile.Tiles.TileSheet sheet = location.map.GetTileSheet(tilesheet.SheetId);
                    foreach (string prop in tilesheet.Properties.Keys)
                        sheet.Properties[prop] = tilesheet.Properties[prop];
                }

            }
            catch (Exception err)
            {
                AdvancedLocationLoaderMod.Logger.ExitGameImmediately($"Unable to patch tilesheet, a unexpected error occured at stage {stage}{(skip > 0 ? ("-" + skip) : "")}{(branch > 0 ? ("-" + branch) : "")}: {tilesheet}", err);
            }
        }
        internal static void ApplyLocation(Location location)
        {
            AdvancedLocationLoaderMod.Logger.Log(location.ToString(),LogLevel.Trace);
            try
            {
                GameLocation loc;
                ContentRegistry.RegisterXnb(location.FileName, location.FileName);
                xTile.Map map = Game1.content.Load<xTile.Map>(location.FileName);
                switch (location.Type)
                {
                    case "Cellar":
                        loc = new StardewValley.Locations.Cellar(map, location.MapName);
                        loc.objects = new SerializableDictionary<Microsoft.Xna.Framework.Vector2, StardewValley.Object>();
                        break;
                    case "BathHousePool":
                        loc = new StardewValley.Locations.BathHousePool(map, location.MapName);
                        break;
                    case "Decoratable":
                        loc = new Locations.DecoratableLocation(map, location.MapName);
                        break;
                    case "Desert":
                        loc = new Locations.Desert(map, location.MapName);
                        break;
                    case "Greenhouse":
                        loc = new Locations.Greenhouse(map, location.MapName);
                        break;
                    case "Sewer":
                        loc = new Locations.Sewer(map, location.MapName);
                        break;
                    default:
                        loc = new GameLocation(map, location.MapName);
                        break;
                }
                loc.isOutdoors = location.Outdoor;
                loc.isFarm = location.Farmable;
                Game1.locations.Add(loc);
            }
            catch (Exception err)
            {
                AdvancedLocationLoaderMod.Logger.ExitGameImmediately("Unable to add custom location, a unexpected error occured: " + location.ToString(),err);
            }
        }
        internal static void ApplyOverride(Override obj)
        {
            AdvancedLocationLoaderMod.Logger.Log(obj.ToString(),LogLevel.Trace);
            try
            {
                ContentRegistry.RegisterXnb(obj.FileName, obj.FileName);
                Game1.locations[Game1.locations.FindIndex(l => l.name == obj.MapName)] = (GameLocation)Activator.CreateInstance(Game1.getLocationFromName(obj.MapName).GetType(), Game1.content.Load<xTile.Map>(obj.FileName), obj.MapName);
            }
            catch (Exception err)
            {
                AdvancedLocationLoaderMod.Logger.ExitGameImmediately("Unable to override location, a unexpected error occured: " +obj.ToString(),err);
            }
        }
    }
}
