/// -----------------------------------------------------------------
///  Dynamic MMO (Dynammo)
///  Dissertation project at University of Derby
/// -----------------------------------------------------------------
///  Written by Timothy Leonard
/// -----------------------------------------------------------------

using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Management;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace Dynammo.Common
{

    /// <summary>
    ///     Contains some general use helper functions for mathmatics.
    /// </summary>
    public static class MathHelper
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="w1"></param>
        /// <param name="h1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="w2"></param>
        /// <param name="h2"></param>
        /// <returns></returns>
        public static bool RectsIntersect(int x1, int y1, int w1, int h1,
                                          int x2, int y2, int w2, int h2)
        {
            Rectangle rect = new Rectangle(x1, y1, w1, h1);
            Rectangle rect2 = new Rectangle(x2, y2, w2, h2);
            return rect.IntersectsWith(rect2);
            //if (x1 >= (x2 + w2) || (x1 + w1) <= x2) return false;
	        //if (y1 >= (y2 + h2) || (y1 + h1) <= y2) return false;
            //return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="w1"></param>
        /// <param name="h1"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static bool RectContains(int x1, int y1, int w1, int h1,
                                        int x, int y)
        {
            Rectangle rect = new Rectangle(x1, y1, w1, h1);
            return rect.Contains(x, y);
        }

    }

}
