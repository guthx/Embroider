using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;
using static Embroider.Enums;

namespace Embroider.Quantizers
{
    public class WuQuantizer : Quantizer
    {
        private const int maxColor = 256;
        private const int red = 2;
        private const int green = 1;
        private const int blue = 0;
        private const int sideSize = 33;
        private const int maxVolume = sideSize * sideSize * sideSize;

        private int[] reds;
        private int[] greens;
        private int[] blues;
        private int[] sums;
        private int[] indices;

        private long[,,] weights;
        private long[,,] momentsRed;
        private long[,,] momentsGreen;
        private long[,,] momentsBlue;
        private float[,,] moments;

        private int[] tag;
        private int[] quantizedPixels;
        private int[] table;

        private int imageSize;

        private WuCube[] cubes;

        private void calculateMoments()
        {
            var area = new long[sideSize];
            var areaRed = new long[sideSize];
            var areaGreen = new long[sideSize];
            var areaBlue = new long[sideSize];
            var area2 = new float[sideSize];

            for (int ri = 1; ri < sideSize; ++ri)
            {
                for (int i = 0; i < sideSize; ++i)
                {
                    area[i] = 0;
                    areaRed[i] = 0;
                    areaGreen[i] = 0;
                    areaBlue[i] = 0;
                    area2[i] = 0;
                }

                for (int gi = 1; gi < sideSize; ++gi)
                {
                    long line = 0;
                    long lineRed = 0;
                    long lineGreen = 0;
                    long lineBlue = 0;
                    float line2 = 0;

                    for (int bi = 1; bi < sideSize; ++bi)
                    {
                        line += weights[ri, gi, bi];
                        lineRed += momentsRed[ri, gi, bi];
                        lineGreen += momentsGreen[ri, gi, bi];
                        lineBlue += momentsBlue[ri, gi, bi];
                        line2 += moments[ri, gi, bi];

                        area[bi] += line;
                        areaRed[bi] += lineRed;
                        areaGreen[bi] += lineGreen;
                        areaBlue[bi] += lineBlue;
                        area2[bi] += line2;

                        weights[ri, gi, bi] = weights[ri - 1, gi, bi] + area[bi];
                        momentsRed[ri, gi, bi] = momentsRed[ri - 1, gi, bi] + areaRed[bi];
                        momentsGreen[ri, gi, bi] = momentsGreen[ri - 1, gi, bi] + areaGreen[bi];
                        momentsBlue[ri, gi, bi] = momentsBlue[ri - 1, gi, bi] + areaBlue[bi];
                        moments[ri, gi, bi] = moments[ri - 1, gi, bi] + area2[bi];
                    }
                }
            }
        }

        private static long volume(WuCube cube, long[,,] moment)
        {
            return moment[cube.Rmax, cube.Gmax, cube.Bmax] -
                   moment[cube.Rmax, cube.Gmax, cube.Bmin] -
                   moment[cube.Rmax, cube.Gmin, cube.Bmax] +
                   moment[cube.Rmax, cube.Gmin, cube.Bmin] -
                   moment[cube.Rmin, cube.Gmax, cube.Bmax] +
                   moment[cube.Rmin, cube.Gmax, cube.Bmin] +
                   moment[cube.Rmin, cube.Gmin, cube.Bmax] -
                   moment[cube.Rmin, cube.Gmin, cube.Bmin];
        }

        private static float volumeFloat(WuCube cube, float[,,] moment)
        {
            return moment[cube.Rmax, cube.Gmax, cube.Bmax] -
                   moment[cube.Rmax, cube.Gmax, cube.Bmin] -
                   moment[cube.Rmax, cube.Gmin, cube.Bmax] +
                   moment[cube.Rmax, cube.Gmin, cube.Bmin] -
                   moment[cube.Rmin, cube.Gmax, cube.Bmax] +
                   moment[cube.Rmin, cube.Gmax, cube.Bmin] +
                   moment[cube.Rmin, cube.Gmin, cube.Bmax] -
                   moment[cube.Rmin, cube.Gmin, cube.Bmin];
        }

        private static long top(WuCube cube, int direction, int position, long[,,] moment)
        {
            switch (direction)
            {
                case red:
                    return moment[position, cube.Gmax, cube.Bmax] -
                        moment[position, cube.Gmax, cube.Bmin] -
                        moment[position, cube.Gmin, cube.Bmax] +
                        moment[position, cube.Gmin, cube.Bmin];

                case green:
                    return moment[cube.Rmax, position, cube.Bmax] -
                        moment[cube.Rmax, position, cube.Bmin] -
                        moment[cube.Rmin, position, cube.Bmax] +
                        moment[cube.Rmin, position, cube.Bmin];
                case blue:
                    return moment[cube.Rmax, cube.Gmax, position] -
                        moment[cube.Rmax, cube.Gmin, position] -
                        moment[cube.Rmin, cube.Gmax, position] +
                        moment[cube.Rmin, cube.Gmin, position];
                default:
                    return 0;
            }
        }

        private static long bottom(WuCube cube, int direction, long[,,] moment)
        {
            switch (direction)
            {
                case red:
                    return -moment[cube.Rmin, cube.Gmax, cube.Bmax] +
                        moment[cube.Rmin, cube.Gmax, cube.Bmin] +
                        moment[cube.Rmin, cube.Gmin, cube.Bmax] -
                        moment[cube.Rmin, cube.Gmin, cube.Bmin];
                case green:
                    return -moment[cube.Rmax, cube.Gmin, cube.Bmax] +
                        moment[cube.Rmax, cube.Gmin, cube.Bmin] +
                        moment[cube.Rmin, cube.Gmin, cube.Bmax] -
                        moment[cube.Rmin, cube.Gmin, cube.Bmin];
                case blue:
                    return -moment[cube.Rmax, cube.Gmax, cube.Bmin] +
                        moment[cube.Rmax, cube.Gmin, cube.Bmin] +
                        moment[cube.Rmin, cube.Gmax, cube.Bmin] -
                        moment[cube.Rmin, cube.Gmin, cube.Bmin];
                default:
                    return 0;

            }
        }

        private float calculateVariance(WuCube cube)
        {
            float volRed = volume(cube, momentsRed);
            float volGreen = volume(cube, momentsGreen);
            float volBlue = volume(cube, momentsBlue);
            float volMoment = volumeFloat(cube, moments);
            float volWeight = volume(cube, weights);

            float distance = volRed * volRed + volGreen * volGreen + volBlue * volBlue;

            return volMoment - (distance / volWeight);
        }

        private float maximize(WuCube cube, int direction, int first, int last, IList<int> cut, long wholeRed, long wholeGreen, long wholeBlue, long wholeWeight)
        {
            long bottomRed = bottom(cube, direction, momentsRed);
            long bottomGreen = bottom(cube, direction, momentsGreen);
            long bottomBlue = bottom(cube, direction, momentsBlue);
            long bottomWeight = bottom(cube, direction, weights);

            float result = 0.0f;
            cut[0] = -1;

            for (int position = first; position < last; ++position)
            {
                long halfRed = bottomRed + top(cube, direction, position, momentsRed);
                long halfGreen = bottomGreen + top(cube, direction, position, momentsGreen);
                long halfBlue = bottomBlue + top(cube, direction, position, momentsBlue);
                long halfWeight = bottomWeight + top(cube, direction, position, weights);

                if (halfWeight != 0)
                {
                    float halfDistance = halfRed * halfRed + halfGreen * halfGreen + halfBlue * halfBlue;
                    float temp = halfDistance / halfWeight;

                    halfRed = wholeRed - halfRed;
                    halfGreen = wholeGreen - halfGreen;
                    halfBlue = wholeBlue - halfBlue;
                    halfWeight = wholeWeight - halfWeight;

                    if (halfWeight != 0)
                    {
                        halfDistance = halfRed * halfRed + halfGreen * halfGreen + halfBlue * halfBlue;
                        temp += halfDistance / halfWeight;
                        if (temp > result)
                        {
                            result = temp;
                            cut[0] = position;
                        }
                    }
                }
            }
            return result;
        }

        private bool cut(WuCube first, WuCube second)
        {
            int direction;

            int[] cutRed = { 0 };
            int[] cutGreen = { 0 };
            int[] cutBlue = { 0 };

            long wholeRed = volume(first, momentsRed);
            long wholeGreen = volume(first, momentsGreen);
            long wholeBlue = volume(first, momentsBlue);
            long wholeWeight = volume(first, weights);

            float maxRed = maximize(first, red, first.Rmin + 1, first.Rmax, cutRed, wholeRed, wholeGreen, wholeBlue, wholeWeight);
            float maxGreen = maximize(first, green, first.Gmin + 1, first.Gmax, cutGreen, wholeRed, wholeGreen, wholeBlue, wholeWeight);
            float maxBlue = maximize(first, blue, first.Bmin + 1, first.Bmax, cutBlue, wholeRed, wholeGreen, wholeBlue, wholeWeight);

            if ((maxRed >= maxGreen) && (maxRed >= maxBlue))
            {
                direction = red;
                if (cutRed[0] < 0)
                    return false;
            }
            else if ((maxGreen >= maxRed) && (maxGreen >= maxBlue))
            {
                direction = green;
            }
            else
            {
                direction = blue;
            }

            second.Rmax = first.Rmax;
            second.Gmax = first.Gmax;
            second.Bmax = first.Bmax;

            switch (direction)
            {
                case red:
                    second.Rmin = first.Rmax = cutRed[0];
                    second.Gmin = first.Gmin;
                    second.Bmin = first.Bmin;
                    break;
                case green:
                    second.Gmin = first.Gmax = cutGreen[0];
                    second.Rmin = first.Rmin;
                    second.Bmin = first.Bmin;
                    break;
                case blue:
                    second.Bmin = first.Bmax = cutBlue[0];
                    second.Rmin = first.Rmin;
                    second.Gmin = first.Gmin;
                    break;
            }

            first.Volume = (first.Rmax - first.Rmin) * (first.Gmax - first.Gmin) * (first.Bmax - first.Bmin);
            second.Volume = (second.Rmax - second.Rmin) * (second.Gmax - second.Gmin) * (second.Bmax - second.Bmin);

            return true;
        }

        private static void mark(WuCube cube, int label, IList<int> tag)
        {
            for (int ri = cube.Rmin + 1; ri <= cube.Rmax; ++ri)
            {
                for (int gi = cube.Gmin + 1; gi <= cube.Gmax; ++gi)
                {
                    for (int bi = cube.Bmin + 1; bi <= cube.Bmax; ++bi)
                    {
                        tag[(ri << 10) + (ri << 6) + ri + (gi << 5) + gi + bi] = label;
                    }
                }
            }
        }

        public WuQuantizer(Image<Rgb24> image,
            DithererType dithererType,
            ColorComparerType colorComparerType,
            int dithererStrength = 255) : base(image, dithererType, colorComparerType, dithererStrength)
        {
        }

        protected override void MakePalette(int paletteSize)
        {
            Palette = new List<Color>();
            cubes = new WuCube[maxColor];
            for (int cubeIndex = 1; cubeIndex < maxColor; cubeIndex++)
            {
                cubes[cubeIndex] = new WuCube();
            }
            cubes[0] = new WuCube
            {
                Rmin = 0,
                Gmin = 0,
                Bmin = 0,
                Rmax = sideSize - 1,
                Gmax = sideSize - 1,
                Bmax = sideSize - 1
            };

            weights = new long[sideSize, sideSize, sideSize];
            momentsRed = new long[sideSize, sideSize, sideSize];
            momentsGreen = new long[sideSize, sideSize, sideSize];
            momentsBlue = new long[sideSize, sideSize, sideSize];
            moments = new float[sideSize, sideSize, sideSize];

            table = new int[maxColor];
            for (int i = 0; i < maxColor; ++i)
            {
                table[i] = i * i;
            }

            imageSize = _image.Width * _image.Height;

            quantizedPixels = new int[imageSize];
            for (int i = 0; i < pixels.Count; ++i)
            {
                int indexRed = ((int)pixels[i].X >> 3) + 1;
                int indexGreen = ((int)pixels[i].Y >> 3) + 1;
                int indexBlue = ((int)pixels[i].Z >> 3) + 1;

                weights[indexRed, indexGreen, indexBlue]++;
                momentsRed[indexRed, indexGreen, indexBlue] += (int)pixels[i].X;
                momentsGreen[indexRed, indexGreen, indexBlue] += (int)pixels[i].Y;
                momentsBlue[indexRed, indexGreen, indexBlue] += (int)pixels[i].Z;
                moments[indexRed, indexGreen, indexBlue] += table[(int)pixels[i].X] + table[(int)pixels[i].Y] + table[(int)pixels[i].Z];

                quantizedPixels[i] = (indexRed << 10) + (indexRed << 6) + indexRed + (indexGreen << 5) + indexGreen + indexBlue;
            }

            calculateMoments();

            int next = 0;
            var volumeVariance = new float[maxColor];
            for (int i = 1; i < paletteSize; ++i)
            {
                if (cut(cubes[next], cubes[i]))
                {
                    volumeVariance[next] = cubes[next].Volume > 1 ? calculateVariance(cubes[next]) : 0.0f;
                    volumeVariance[i] = cubes[i].Volume > 1 ? calculateVariance(cubes[i]) : 0.0f;
                }
                else
                {
                    volumeVariance[next] = 0.0f;
                    i--;
                }

                next = 0;
                float temp = volumeVariance[0];
                for (int j = 1; j <= i; ++j)
                {
                    if (volumeVariance[j] > temp)
                    {
                        temp = volumeVariance[j];
                        next = j;
                    }
                }

                if (temp <= 0.0)
                {
                    paletteSize = i + 1;
                    break;
                }
            }

            var lookupRed = new int[maxColor];
            var lookupGreen = new int[maxColor];
            var lookupBlue = new int[maxColor];
            tag = new int[maxVolume];

            for (int k = 0; k < paletteSize; k++)
            {
                mark(cubes[k], k, tag);
                long weight = volume(cubes[k], weights);

                if (weight > 0)
                {
                    lookupRed[k] = (int)(volume(cubes[k], momentsRed) / weight);
                    lookupGreen[k] = (int)(volume(cubes[k], momentsGreen) / weight);
                    lookupBlue[k] = (int)(volume(cubes[k], momentsBlue) / weight);
                }
                else
                {
                    lookupRed[k] = 0;
                    lookupGreen[k] = 0;
                    lookupBlue[k] = 0;
                }
            }
            for (int i = 0; i < imageSize; i++)
            {
                quantizedPixels[i] = tag[quantizedPixels[i]];
            }

            reds = new int[paletteSize + 1];
            greens = new int[paletteSize + 1];
            blues = new int[paletteSize + 1];
            sums = new int[paletteSize + 1];
            indices = new int[imageSize];

            for (int i = 0; i < imageSize; i++)
            {
                var color = pixels[i];
                int match = quantizedPixels[i];
                int bestMatch = match;
                int bestDistance = 100000000;
                for (int j = 0; j < paletteSize; j++)
                {
                    int foundRed = lookupRed[j];
                    int foundGreen = lookupGreen[j];
                    int foundBlue = lookupBlue[j];
                    int deltaRed = (int)color.X - foundRed;
                    int deltaGreen = (int)color.Y - foundGreen;
                    int deltaBlue = (int)color.Z - foundBlue;
                    int distance = deltaRed * deltaRed + deltaGreen * deltaGreen + deltaBlue * deltaBlue;

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestMatch = j;
                    }
                }

                reds[bestMatch] += (int)color.X;
                greens[bestMatch] += (int)color.Y;
                blues[bestMatch] += (int)color.Z;
                sums[bestMatch]++;
                indices[i] = bestMatch;
            }

            for (int i = 0; i < paletteSize; i++)
            {
                if (sums[i] > 0)
                {
                    reds[i] /= sums[i];
                    greens[i] /= sums[i];
                    blues[i] /= sums[i];
                }

                Palette.Add(new Color(reds[i], greens[i], blues[i]));
            }
        }
    }
}
