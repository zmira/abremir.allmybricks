﻿using abremir.AllMyBricks.Onboarding.Shared.Enumerations;
using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

[assembly: InternalsVisibleTo("abremir.AllMyBricks.Onboarding.Tests")]

namespace abremir.AllMyBricks.Onboarding.Helpers
{
    internal static class RandomKeyOptionGenerator
    {
        internal static AlgorithmType GetRandomKeyOption()
        {
            const int min = (int)AlgorithmType.Type1;
            const int max = (int)AlgorithmType.Type3 + 1;

            using var rng = new RNGCryptoServiceProvider();

            var data = new byte[4];
            rng.GetBytes(data);

            int generatedValue = Math.Abs(BitConverter.ToInt32(data, startIndex: 0));

            const int diff = max - min;
            int mod = generatedValue % diff;
            return (AlgorithmType)(min + mod);
        }
    }
}
