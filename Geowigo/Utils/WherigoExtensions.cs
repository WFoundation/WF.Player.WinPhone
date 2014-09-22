using System;
using System.Device.Location;
using WF.Player.Core;
using Microsoft.Phone.Controls.Maps;
using System.Collections.Generic;

namespace Geowigo.Utils
{
	public static class WherigoExtensions
	{
		/// <summary>
		/// Gets a displayable full name of the author of this Cartridge.
		/// </summary>
		/// <param name="c"></param>
		/// <returns>The author name and company, aggregated in a presentable
		/// way.</returns>
		public static string GetFullAuthor(this Cartridge c)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			bool hasAuthor = !String.IsNullOrWhiteSpace(c.AuthorName);
			bool hasCompany = !String.IsNullOrWhiteSpace(c.AuthorCompany)
				&& c.AuthorCompany != c.AuthorName;
			if (hasAuthor || hasCompany)
			{
				if (hasAuthor)
				{
					sb.Append(c.AuthorName);
					if (hasCompany)
					{
						sb.Append(" (");
					}
				}
				if (hasCompany)
				{
					sb.Append(c.AuthorCompany);
					if (hasAuthor)
					{
						sb.Append(")");
					}
				}
			}

			return sb.ToString();
		}
		
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
		/// Gets a ZonePoint representing this GeoCoordinate.
		/// </summary>
		/// <param name="gc"></param>
		/// <returns></returns>
		public static ZonePoint ToZonePoint(this GeoCoordinate gc)
		{
			return new ZonePoint(gc.Latitude, gc.Longitude, gc.Altitude);
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
		/// Gets a LocationCollection representing an enumeration of points.
		/// </summary>
		/// <param name="ezp"></param>
		/// <returns></returns>
		public static LocationCollection ToLocationCollection(this IEnumerable<ZonePoint> ezp)
		{
			LocationCollection coll = new LocationCollection();

			foreach (ZonePoint p in ezp)
			{
				coll.Add(p.ToGeoCoordinate());
			}

			return coll;
		}
	}
}
