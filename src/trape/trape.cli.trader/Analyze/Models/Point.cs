using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using trape.cli.trader.Cache;

namespace trape.cli.trader.Analyze.Models
{
    /// <summary>
    /// This class implements functionality for <c>Point</c>s.
    /// </summary>
    public class Point : IComparable<Point>
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Point</c>
        /// </summary>
        /// <param name="time">Time (X)</param>
        /// <param name="price">Price (Y)</param>
        /// <param name="slope">Slope</param>
        /// <param name="slopeBase">Timespan of slope in seconds</param>
        public Point(TimeSpan time = default, decimal price = 0, decimal slope = 0, TimeSpan slopeBase = default)
        {
            this.Time = time;
            this.Price = price;
            this.Slope = slope;

            if (slopeBase.TotalSeconds < 1)
            {
                slopeBase = TimeSpan.FromSeconds(1);
            }

            this.SlopeBase = slopeBase == default ? TimeSpan.FromSeconds(1) : slopeBase;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Time (X)
        /// </summary>
        public TimeSpan Time { get; private set; }

        /// <summary>
        /// Price (Y)
        /// </summary>
        public decimal Price { get; private set; }

        /// <summary>
        /// Slope
        /// </summary>
        public decimal Slope { get; private set; }

        /// <summary>
        /// Base of slope
        /// </summary>
        public TimeSpan SlopeBase { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Checks if this <c>Point</c> is in the vicinity of the <paramref name="other"/> one.
        /// Vicinity is an overlap by 0.05% of each <see cref="Price"/> value.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsTouching([AllowNull] Point other)
        {
            if (other == null)
            {
                return false;
            }

            const decimal lower = 0.9998M;
            const decimal higher = 1.0002M;

            if (this.Price > other.Price)
            {
                return (other.Price * higher) >= (this.Price * lower);
            }
            else if (this.Price < other.Price)
            {
                return (this.Price * higher) >= (other.Price * lower);
            }

            // Same point
            return true;
        }

        /// <summary>
        /// Checks if this point is between <paramref name="point1"/> and <paramref name="point2"/>. If <paramref name="isTtouching"/> is true,
        /// this point may be a bit higher/lower than point1/point2
        /// </summary>
        /// <param name="point1">A point (higher or lower)</param>
        /// <param name="point2">A point (higher or lower)</param>
        /// <param name="isTtouching">If true, matching is a bit fuzzy</param>
        /// <returns></returns>
        public bool IsBetween(Point point1, Point point2, bool isTtouching)
        {
            Point higherPoint, lowerPoint;
            if (point1 < point2)
            {
                higherPoint = point2;
                lowerPoint = point1;
            }
            else
            {
                higherPoint = point1;
                lowerPoint = point2;
            }

            var result = this > lowerPoint && this < higherPoint;

            if (!result && isTtouching)
            {
                result = this.IsTouching(lowerPoint) || this.IsTouching(higherPoint);
            }

            return result;
        }

        /// <summary>
        /// Calculates the <c>Point</c> where this instance will intercept <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The other point</param>
        /// <returns>Returns point where interception will take place or null in case it does not.</returns>
        public Point WillInterceptWith(Point other)
        {
            var thisSlopeInSec = this.Slope / (decimal)this.SlopeBase.TotalSeconds;
            var otherSlopeInSec = other.Slope / (decimal)other.SlopeBase.TotalSeconds;

            // Never intercept
            if (this.Price < other.Price && thisSlopeInSec < otherSlopeInSec)
            {
                return null;
            }
            else if (this.Price > other.Price && thisSlopeInSec > otherSlopeInSec)
            {
                return null;
            }
            else if (this.Slope == other.Slope)
            {
                return null;
            }

            try
            {
                // Calculate Intercept
                var iTime = (this.Price - other.Price) / (otherSlopeInSec - thisSlopeInSec);

                var iPrice = this.Price + thisSlopeInSec * iTime;

                return new Point(TimeSpan.FromSeconds((int)iTime), iPrice, 0);
            }
            catch
            {

            }

            return null;
        }

        /// <summary>
        /// Calculates the <c>Point</c> where this instance will intercept the line of point <paramref name="price"/> and slope <paramref name="slope"/>.
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="slope">Slope in seconds!</param>
        /// <returns></returns>
        public Point WillInterceptWith(decimal price, decimal slope = 0, int slopeBase = 1)
        {
            if (slopeBase < 1)
            {
                slopeBase = 1;
            }

            return this.WillInterceptWith(new Point(price: price, slope: slope, slopeBase: TimeSpan.FromSeconds(slopeBase)));
        }

        #endregion

        #region Operators

        /// <summary>
        /// Compares the <see cref="Price"/> of this <c>Point</c> against the <see cref="Price"/> of the <paramref name="other"/> one.
        /// </summary>
        /// <param name="other">Point</param>
        /// <returns></returns>
        public int CompareTo([AllowNull] Point other)
        {
            return compare(this, other);
        }

        /// <summary>
        /// Internal compare of <see cref="Price"/> values.
        /// </summary>
        /// <param name="p1">Point 1</param>
        /// <param name="p2">Point 2</param>
        /// <returns></returns>
        private static int compare([AllowNull]Point p1, [AllowNull]Point p2)
        {
            if (p1 == null && p2 == null)
            {
                return 0;
            }
            else if (p1 == null)
            {
                return -1;
            }
            else if (p2 == null)
            {
                return 1;
            }

            if (p1.Price > p2.Price)
            {
                return 1;
            }
            else if (p1.Price < p2.Price)
            {
                return -1;
            }

            // Same
            return 0;
        }

        /// <summary>
        /// Operator < (less than)
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static bool operator <(Point p1, Point p2)
        {
            return compare(p1, p2) < 0;
        }

        /// <summary>
        /// Operator > (greater than)
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static bool operator >(Point p1, Point p2)
        {
            return compare(p1, p2) > 0;
        }

        /// <summary>
        /// Operatpr *
        /// </summary>
        /// <param name="p"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Point operator *(Point p, decimal c)
        {
            return new Point(default, p.Price * c, 0);
        }

        /// <summary>
        /// Operatpr *
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static Point operator *(Point p1, Point p2)
        {
            return new Point(default, p1.Price * p2.Price, 0);
        }

        /// <summary>
        /// Operatpr /
        /// </summary>
        /// <param name="p"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Point operator /(Point p, decimal c)
        {
            return new Point(default, p.Price / c, default);
        }

        /// <summary>
        /// Operatpr /
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static Point operator /(Point p1, Point p2)
        {
            return new Point(default, p1.Price / p2.Price, 0);
        }

        /// <summary>
        /// Operator ==
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static bool operator ==(Point p1, Point p2)
        {
            if (object.ReferenceEquals(p1, null) && object.ReferenceEquals(p2, null))
            {
                return true;
            }
            else if (object.ReferenceEquals(p1, null) || object.ReferenceEquals(p2, null))
            {
                return false;
            }

            return p1.Price == p2.Price;
        }

        /// <summary>
        /// Operator !=
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static bool operator !=(Point p1, Point p2)
        {
            if (object.ReferenceEquals(p1, null) && object.ReferenceEquals(p2, null))
            {
                return false;
            }
            else if (object.ReferenceEquals(p1, null) || object.ReferenceEquals(p2, null))
            {
                return true;
            }

            return p1.Price != p2.Price;
        }

        /// <summary>
        /// Operatpr >=
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static bool operator >=(Point p1, Point p2)
        {
            if (p1 == null && p2 == null)
            {
                return true;
            }
            else if (p1 == null || p2 == null)
            {
                return false;
            }

            return p1.Price >= p2.Price;
        }

        /// <summary>
        /// Operatpr <=
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static bool operator <=(Point p1, Point p2)
        {
            return p1.Price <= p2.Price;
        }

        /// <summary>
        /// Operatpr -
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static Point operator -(Point p1, Point p2)
        {
            return new Point(default, p1.Price - p2.Price, 0);
        }


        /// <summary>
        /// Operatpr +
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static Point operator +(Point p1, Point p2)
        {
            return new Point(default, p1.Price + p2.Price, 0);
        }

        /// <summary>
        /// Returns the price
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Price.ToString();
        }

        #endregion
    }
}
