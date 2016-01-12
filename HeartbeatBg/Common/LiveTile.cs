using System;
using System.Linq;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;

namespace Common
{
    public class LiveTile
    {
        public static string idString = "com.drjukka.heartbeatBg.secondaryTile";
        public static string appName = "HeartbeatBG";

        static public string GetXMLForTile(string beatValue)
        {
            string xml = "<tile version='3'>"
            + "<visual>"
            + "<binding template='TileSmall'>"
            + "<text hint-style='subtitle'>" + beatValue + "</text>"
            + "<text hint-style='captionSubtle'>Bmp</text>"
            + "</binding>"
            + "<binding template='TileMedium'>"
            + "<text hint-style='header'>" + beatValue + "</text>"
            + "<text hint-style='titleSubtle'>Bmp</text>"
            + "</binding>"
            + "<binding template='TileWide'>"
            + "<group>"
            + "<subgroup hint-weight='33'>"
            + "<image src='Assets/StoreLogo.png' hint-crop='circle'/>"
            + "</subgroup>"
            + "<subgroup hint-textStacking='center'>"
            + "<text hint-style='header'>" + beatValue + "</text>"
            + "<text hint-style='titleSubtle'>Bmp</text>"
            + "</subgroup>"
            + "</group>"
            + "</binding>"
            + "<binding template='TileLarge'>"
            + "<text hint-style='header'>" + beatValue + "</text>"
            + "<text hint-style='titleSubtle'>Bmp</text>"
            + "</binding>"
            + "</visual>"
            + "</tile>";

            return xml;
        }

        static public async void CreateSecoondaryTile()
        {
            SecondaryTile tile = new SecondaryTile(idString, appName, "args", new Uri("ms-appx:///Assets/Logo.png"), TileSize.Default);
            tile.VisualElements.Square71x71Logo = new Uri("ms-appx:///Assets/Small.png");
            tile.VisualElements.Wide310x150Logo = new Uri("ms-appx:///Assets/WideLogo.png");
            tile.VisualElements.Square310x310Logo = new Uri("ms-appx:///Assets/LargeLogo.png");
            tile.VisualElements.Square44x44Logo = new Uri("ms-appx:///Assets/SmallLogo.png"); // Branding logo

            tile.VisualElements.ShowNameOnSquare150x150Logo = true;
            tile.VisualElements.ShowNameOnSquare310x310Logo = true;
            tile.VisualElements.ShowNameOnWide310x150Logo = true;

            await tile.RequestCreateAsync();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(GetXMLForTile("XX"));

            TileNotification notification = new TileNotification(doc);
            TileUpdateManager.CreateTileUpdaterForSecondaryTile(tile.TileId).Update(notification);
        }

        static public async void UpdateSecondaryTile(string beatValue)
        {
            try
            {
                SecondaryTile tile = (await SecondaryTile.FindAllAsync()).FirstOrDefault(i => i.TileId.Equals(idString));
                if (tile != null)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(GetXMLForTile(beatValue));

                    // Create the notification
                    TileNotification notifyTile = new TileNotification(xmlDoc);
                    // And send the notification to the tile
                    TileUpdateManager.CreateTileUpdaterForSecondaryTile(tile.TileId).Update(notifyTile);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error updating tile : " + ex.ToString());
            }
        }
    }
}
