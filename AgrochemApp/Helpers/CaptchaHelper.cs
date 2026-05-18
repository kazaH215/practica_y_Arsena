using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AgrochemApp.Helpers
{
    public class CaptchaHelper
    {
        private string _captchaText;
        private Random _rand = new Random();

        public string CaptchaText => _captchaText;

        public BitmapSource GenerateCaptcha()
        {
            _captchaText = _rand.Next(1000, 9999).ToString();

            // Создаём картинку 150x50
            var bitmap = new WriteableBitmap(150, 50, 96, 96, PixelFormats.Bgr32, null);

            // Массив пикселей (150 * 50 * 4 байта на пиксель)
            byte[] pixels = new byte[150 * 50 * 4];

            // Заполняем белым фоном
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = 255;     // Blue
                pixels[i + 1] = 255; // Green
                pixels[i + 2] = 255; // Red
                pixels[i + 3] = 255; // Alpha
            }

            // Рисуем цифры
            for (int i = 0; i < _captchaText.Length; i++)
            {
                char c = _captchaText[i];
                int x = i * 35 + 15;
                int y = 10;

                // Просто закрашиваем область для каждой цифры
                for (int dx = 0; dx < 25; dx++)
                {
                    for (int dy = 0; dy < 35; dy++)
                    {
                        int px = x + dx;
                        int py = y + dy;
                        if (px < 150 && py < 50)
                        {
                            int index = (py * 150 + px) * 4;
                            if (index + 2 < pixels.Length)
                            {
                                // Цифры тёмно-синие
                                pixels[index] = 100;
                                pixels[index + 1] = 80;
                                pixels[index + 2] = 180;
                            }
                        }
                    }
                }
            }

            // Добавляем шум (случайные точки)
            for (int i = 0; i < 500; i++)
            {
                int x = _rand.Next(0, 150);
                int y = _rand.Next(0, 50);
                int index = (y * 150 + x) * 4;
                if (index + 2 < pixels.Length)
                {
                    pixels[index] = (byte)_rand.Next(50, 200);
                    pixels[index + 1] = (byte)_rand.Next(50, 200);
                    pixels[index + 2] = (byte)_rand.Next(50, 200);
                }
            }

            bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, 150, 50), pixels, 150 * 4, 0);

            return bitmap;
        }
    }
}