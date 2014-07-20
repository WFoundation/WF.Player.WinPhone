using System;
using System.Device.Location;
using WF.Player.Core;
using Microsoft.Phone.Controls.Maps;

namespace Geowigo.Utils
{
	public static class WherigoExtensions
	{
		/// <summary>
		/// Gets a GeoCoordinate representing this ZonePoint.
		/// </summary>
		/// <param name="zp"></param>
		/// <returns></returns>
		public static GeoCoordinate ToGeoCoordinate(this ZonePoint zp)
		{
			return new GeoCoordinate(zp.Latitude, zp.Longitude, zp.Altitude);
		}

		/// <summary>
		/// Gets a LocationRect representing these CoordBounds.
		/// </summary>
		/// <param name="cb"></param>
		/// <returns></returns>
		public static LocationRect ToLocationRect(this CoordBounds cb)
		{
			return new LocationRect(cb.Top, cb.Left, cb.Bottom, cb.Right);
		}

		/// <summary>
		/// Gets a LocationCollection representing the points of this Zone.
		/// </summary>
		/// <param name="z"></param>
		/// <returns></returns>
		public static LocationCollection ToLocationCollection(this Zone z)
		{
			LocationCollection coll = new LocationCollection();

			foreach (ZonePoint p in z.Points)
			{
				coll.Add(p.ToGeoCoordinate());
			}

			return coll;
		}
	}
}
