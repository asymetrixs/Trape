using System;
using System.Diagnostics.CodeAnalysis;

namespace Trape.Cli.trader.Analyze.Models
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
            Time = time;
            Value = price;
            Slope = slope;

            if (slopeBase.TotalSeconds < 1)
            {
                slopeBase = TimeSpan.FromSeconds(1);
            }

            SlopeBase = slopeBase == default ? TimeSpan.FromSeconds(1) : slopeBase;
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
        public decimal Value { get; private set; }

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
        /// Vicinity is an overlap by 0.05% of each <see cref="Value"/> value.
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

            if (Value > other.Value)
            {
                return (other.Value * higher) >= (Value * lower);
            }
            else if (Value < other.Value)
            {
                return (Value * higher) >= (other.Value * lower);
            }

            // Same point
            return true;
        }

        /// <summary>
        /// Checks if this <c>Point</c> is close to the <paramref name="other"/> one.
        /// Close means an a distance of 0.1% of each <see cref="Value"/> value or less.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsClose([AllowNull] Point other)
        {
            if (other == null)
            {
                return false;
            }

            const decimal lower = 0.999M;
            const decimal higher = 1.001M;

            if (Value > other.Value)
            {
                return (other.Value * higher) >= (Value * lower);
            }
            else if (Value < other.Value)
            {
                return (Value * higher) >= (other.Value * lower);
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
                result = IsTouching(lowerPoint) || IsTouching(higherPoint);
            }

            return result;
        }

        /// <summary>
        /// Calculates the <c>Point</c> where this instance will intercept <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The other point</param>
        /// <returns>Returns point where interception will take place or null in case it does not.</returns>
        public Point WillInterceptWith([AllowNull] Point other)
        {
            if (other == null)
            {
                return null;
            }

            var thisSlopeInSec = Slope / (decimal)SlopeBase.TotalSeconds;
            var otherSlopeInSec = other.Slope / (decimal)other.SlopeBase.TotalSeconds;

            // Never intercept
            if (Value < other.Value && thisSlopeInSec < otherSlopeInSec)
            {
                return null;
            }
            else if (Value > other.Value && thisSlopeInSec > otherSlopeInSec)
            {
                return null;
            }
            else if (Slope == other.Slope)
            {
                return null;
            }

            try
            {
                // Calculate Intercept
                var iTime = (Value - other.Value) / (otherSlopeInSec - thisSlopeInSec);

                var iPrice = Value + thisSlopeInSec * iTime;

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

            return WillInterceptWith(new Point(price: price, slope: slope, slopeBase: TimeSpan.FromSeconds(slopeBase)));
        }

        #endregion

        #region Operators

        /// <summary>
        /// Compares the <see cref="Value"/> of this <c>Point</c> against the <see cref="Value"/> of the <paramref name="other"/> one.
        /// </summary>
        /// <param name="other">Point</param>
        /// <returns></returns>
        public int CompareTo([AllowNull] Point other)
        {
            return Compare(this, other);
        }

        /// <summary>
        /// Internal compare of <see cref="Value"/> values.
        /// </summary>
        /// <param name="p1">Point 1</param>
        /// <param name="p2">Point 2</param>
        /// <returns></returns>
        private static int Compare([AllowNull] Point p1, [AllowNull] Point p2)
        {
            if (p1 is null && p2 is null)
            {
                return 0;
            }
            else if (p1 is null)
            {
                return -1;
            }
            else if (p2 is null)
            {
                return 1;
            }

            if (p1.Value > p2.Value)
            {
                return 1;
            }
            else if (p1.Value < p2.Value)
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
            return Compare(p1, p2) < 0;
        }

        /// <summary>
        /// Operator > (greater than)
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static bool operator >(Point p1, Point p2)
        {
            return Compare(p1, p2) > 0;
        }

        /// <summary>
        /// Operatpr *
        /// </summary>
        /// <param name="p"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Point operator *(Point p, decimal c)
        {
            decimal pValue = 0;
            if (p != null)
            {
                pValue = p.Value;
            }
            return new Point(default, pValue * c, 0);
        }

        /// <summary>
        /// Operatpr *
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static Point operator *(Point p1, Point p2)
        {
            decimal p1Value = 0, p2Value = 0;

            if (p1 != null)
            {
                p1Value = p1.Value;
            }

            if (p2 != null)
            {
                p2Value = p2.Value;
            }

            return new Point(default, p1Value * p2Value, 0);
        }

        /// <summary>
        /// Operatpr /
        /// </summary>
        /// <param name="p"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Point operator /(Point p, decimal c)
        {
            decimal pValue = 0;
            if (p != null)
            {
                pValue = p.Value;
            }

            return new Point(default, pValue / c, default);
        }

        /// <summary>
        /// Operatpr /
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static Point operator /(Point p1, Point p2)
        {
            decimal p1Value = 0, p2Value = 0;

            if (p1 != null)
            {
                p1Value = p1.Value;
            }

            if (p2 != null)
            {
                p2Value = p2.Value;
            }

            return new Point(default, p1Value / p2Value, 0);
        }

        /// <summary>
        /// Operator ==
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static bool operator ==(Point p1, Point p2)
        {
            return p1?.Value == p2?.Value;
        }

        /// <summary>
        /// Operator !=
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static bool operator !=(Point p1, Point p2)
        {
            if (p1 is null && p2 is null)
            {
                return false;
            }
            else if (p1 is null || p2 is null)
            {
                return true;
            }

            return p1.Value != p2.Value;
        }

        /// <summary>
        /// Operatpr >=
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static bool operator >=(Point p1, Point p2)
        {
            if (p1 is null && p2 is null)
            {
                return true;
            }
            else if (p1 is null)
            {
                return false;
            }
            else if (p2 is null)
            {
                return true;
            }

            return p1.Value >= p2.Value;
        }

        /// <summary>
        /// Operatpr <=
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static bool operator <=(Point p1, Point p2)
        {
            if (p1 is null && p2 is null)
            {
                return false;
            }
            else if (p1 is null)
            {
                return true;
            }
            else if (p2 is null)
            {
                return false;
            }

            return p1.Value <= p2.Value;
        }

        /// <summary>
        /// Operatpr -
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static Point operator -(Point p1, Point p2)
        {
            _ = p1 ?? throw new ArgumentNullException(paramName: nameof(p1));
            _ = p2 ?? throw new ArgumentNullException(paramName: nameof(p2));

            return new Point(default, p1.Value - p2.Value, 0);
        }


        /// <summary>
        /// Operatpr +
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static Point operator +(Point p1, Point p2)
        {
            _ = p1 ?? throw new ArgumentNullException(paramName: nameof(p1));
            _ = p2 ?? throw new ArgumentNullException(paramName: nameof(p2));

            return new Point(default, p1.Value + p2.Value, 0);
        }

        /// <summary>
        /// Returns the price
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is null)
            {
                return false;
            }

            return (obj as Point)?.Value == Value;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        #endregion
    }
}
