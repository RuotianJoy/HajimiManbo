using System;

namespace HajimiManbo.World
{
    /// <summary>
    /// 噪声生成器，用于地形生成
    /// </summary>
    public static class NoiseGenerator
    {
        private static Random random;
        
        /// <summary>
        /// 获取随机数生成器
        /// </summary>
        public static Random Random => random ?? (random = new Random());
        
        /// <summary>
        /// 初始化噪声生成器
        /// </summary>
        public static void Initialize(int seed)
        {
            random = new Random(seed);
        }
        
        /// <summary>
        /// 生成1D Perlin噪声
        /// </summary>
        public static float Noise1D(float x, float frequency = 1f, int octaves = 1, float persistence = 0.5f)
        {
            float value = 0f;
            float amplitude = 1f;
            float maxValue = 0f;
            
            for (int i = 0; i < octaves; i++)
            {
                value += Perlin1D(x * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= 2f;
            }
            
            return value / maxValue;
        }
        
        /// <summary>
        /// 生成2D Perlin噪声
        /// </summary>
        public static float Noise2D(float x, float y, float frequency = 1f, int octaves = 1, float persistence = 0.5f)
        {
            float value = 0f;
            float amplitude = 1f;
            float maxValue = 0f;
            
            for (int i = 0; i < octaves; i++)
            {
                value += Perlin2D(x * frequency, y * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= 2f;
            }
            
            return value / maxValue;
        }
        
        /// <summary>
        /// 简化的1D Perlin噪声实现
        /// </summary>
        private static float Perlin1D(float x)
        {
            int xi = (int)Math.Floor(x);
            float xf = x - xi;
            
            float a = GetPseudoRandom(xi);
            float b = GetPseudoRandom(xi + 1);
            
            return Lerp(a, b, Fade(xf));
        }
        
        /// <summary>
        /// 简化的2D Perlin噪声实现
        /// </summary>
        private static float Perlin2D(float x, float y)
        {
            int xi = (int)Math.Floor(x);
            int yi = (int)Math.Floor(y);
            float xf = x - xi;
            float yf = y - yi;
            
            float a = GetPseudoRandom2D(xi, yi);
            float b = GetPseudoRandom2D(xi + 1, yi);
            float c = GetPseudoRandom2D(xi, yi + 1);
            float d = GetPseudoRandom2D(xi + 1, yi + 1);
            
            float i1 = Lerp(a, b, Fade(xf));
            float i2 = Lerp(c, d, Fade(xf));
            
            return Lerp(i1, i2, Fade(yf));
        }
        
        /// <summary>
        /// 获取伪随机值（1D）
        /// </summary>
        private static float GetPseudoRandom(int x)
        {
            x = (x << 13) ^ x;
            return (1f - ((x * (x * x * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f);
        }
        
        /// <summary>
        /// 获取伪随机值（2D）
        /// </summary>
        private static float GetPseudoRandom2D(int x, int y)
        {
            int n = x + y * 57;
            n = (n << 13) ^ n;
            return (1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f);
        }
        
        /// <summary>
        /// 线性插值
        /// </summary>
        private static float Lerp(float a, float b, float t)
        {
            return a + t * (b - a);
        }
        
        /// <summary>
        /// 平滑函数
        /// </summary>
        private static float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }
        
        /// <summary>
        /// 生成随机浮点数
        /// </summary>
        public static float NextFloat()
        {
            return (float)random.NextDouble();
        }
        
        /// <summary>
        /// 生成指定范围内的随机浮点数
        /// </summary>
        public static float NextFloat(float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
        
        /// <summary>
        /// 生成随机整数
        /// </summary>
        public static int NextInt(int min, int max)
        {
            return random.Next(min, max);
        }
    }
}