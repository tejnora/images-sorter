using System;

namespace ImagesSorter
{
    class Rational
    {
        readonly int _n;
        readonly int _d;

        public Rational(int n, int d)
        {
            _n = n;
            _d = d;
            Simplify(ref n, ref d);
        }

        public Rational(uint n, uint d)
        {
            _n = Convert.ToInt32(n);
            _d = Convert.ToInt32(d);
            Simplify(ref _n, ref _d);
        }

        public Rational()
        {
            _n = _d = 0;
        }

        public string ToString(string sp)
        {
            if (sp == null) sp = "/";
            return _n + sp + _d;
        }

        public double ToDouble()
        {
            if (_d == 0)
                return 0.0;

            return Math.Round(Convert.ToDouble(_n) / Convert.ToDouble(_d), 2);
        }

        static void Simplify(ref int a, ref int b)
        {
            if (a == 0 || b == 0)
                return;

            var gcd = Euclid(a, b);
            a /= gcd;
            b /= gcd;
        }

        static int Euclid(int a, int b)
        {
            if (b == 0)
                return a;
            return Euclid(b, a % b);
        }
    }
}