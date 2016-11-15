﻿//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.IdentityModel.Tokens.Tests
{
    /// <summary>
    /// Tests for KeyWrapProvider
    /// Constructors
    ///     - validate parameters (null, empty)
    ///     - algorithms supported
    ///     - properties are set correctly (Algorithm, Context, Key)
    /// WrapKey/UnwrapKey
    ///     - positive tests for keys Algorithms supported.
    ///     - parameter validation for WrapKey
    /// UnwrapKey
    ///     - parameter validataion for UnwrapKey
    /// UnwrapKeyMismatch
    ///     - negative tests for switching (keys, algorithms)
    /// WrapKeyVirtual
    ///     - tests virtual method was called
    /// UnwrapKeyVirtual
    ///     - tests virtual method was called
    /// </summary>
    public class RsaKeyWrapProviderTests
    {
#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [Theory, MemberData("RsaKeyWrapConstructorTheoryData")]
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
        public void Constructors(string testId, SecurityKey key, string algorithm, bool isDecrypt, ExpectedException ee)
        {
            try
            {
                var context = Guid.NewGuid().ToString();
                var provider = new RsaKeyWrapProvider(key, algorithm, isDecrypt) { Context = context };

                ee.ProcessNoException();

                Assert.Equal(provider.Algorithm, algorithm);
                Assert.Equal(provider.Context, context);
                Assert.True(ReferenceEquals(provider.Key, key));
            }
            catch (Exception ex)
            {
                ee.ProcessException(ex);
            }
        }

        public static TheoryData<string, SecurityKey, string, bool, ExpectedException> RsaKeyWrapConstructorTheoryData()
        {
            var theoryData = new TheoryData<string, SecurityKey, string, bool, ExpectedException>();

            theoryData.Add("Test1", null, null, false, ExpectedException.ArgumentNullException());
            theoryData.Add("Test2", KeyingMaterial.RsaSecurityKey_2048, null, false, ExpectedException.ArgumentNullException());
            theoryData.Add("Test3", KeyingMaterial.RsaSecurityKey_1024, SecurityAlgorithms.RsaOAEP, false, ExpectedException.ArgumentOutOfRangeException("IDX10662:"));
            theoryData.Add("Test4", KeyingMaterial.RsaSecurityKey_2048, SecurityAlgorithms.Aes128KW, false, ExpectedException.ArgumentException("IDX10671:"));
            theoryData.Add("Test5", KeyingMaterial.RsaSecurityKey_2048, SecurityAlgorithms.RsaPKCS1, false, ExpectedException.NoExceptionExpected);
         //   theoryData.Add("Test6", KeyingMaterial.X509SecurityKey2, SecurityAlgorithms.RsaPKCS1, false, ExpectedException.NoExceptionExpected);

            JsonWebKey webKey = KeyingMaterial.JsonWebKeyRsa256;
            theoryData.Add("Test7", webKey, SecurityAlgorithms.RsaPKCS1, true, ExpectedException.NoExceptionExpected);
            webKey = KeyingMaterial.JsonWebKeyRsa256Public;
            theoryData.Add("Test8", webKey, SecurityAlgorithms.RsaPKCS1, true, ExpectedException.ArgumentNullException("IDX10702:"));

            return theoryData;
        }

        [Fact]
        public void RsaKeyWrapProviderDispose_Test()
        {
            SecurityKey key = KeyingMaterial.RsaSecurityKey_2048;
            var provider = new RsaKeyWrapProvider(key, SecurityAlgorithms.RsaPKCS1, false);
            key.CryptoProviderFactory.ReleaseRsaKeyWrapProvider(provider);
        }

        [Fact]
        public void UnwrapKey()
        {
            var provider = new DerivedRsaKeyWrapProvider(KeyingMaterial.RsaSecurityKey_2048, SecurityAlgorithms.RsaPKCS1, true);
            byte[] wrappedKey = provider.WrapKey(Guid.NewGuid().ToByteArray());
            provider.UnwrapKey(wrappedKey);
            Assert.True(provider.UnwrapKeyCalled);
        }

        [Fact]
        public void WrapKey()
        {
            var provider = new DerivedRsaKeyWrapProvider(KeyingMaterial.RsaSecurityKey1, SecurityAlgorithms.RsaPKCS1, false);
            byte[] wrappedKey = provider.WrapKey(Guid.NewGuid().ToByteArray());
            Assert.True(provider.WrapKeyCalled);
        }

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [Theory, MemberData("RsaWrapUnwrapTheoryData")]
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
        public void RsaWrapUnwrapKey(RsaKeyWrapTestParams theoryParams)
        {
            try
            {
                var encryptProvider = new RsaKeyWrapProvider(theoryParams.EncryptKey, theoryParams.EncryptAlgorithm, false);
                byte[] wrappedKey = encryptProvider.WrapKey(theoryParams.KeyToWrap);
                var decryptProvider = new DerivedRsaKeyWrapProvider(theoryParams.DecryptKey, theoryParams.DecryptAlgorithm, true);
                byte[] unwrappedKey = decryptProvider.UnwrapKey(wrappedKey);

                Assert.True(Utility.AreEqual(unwrappedKey, theoryParams.KeyToWrap), "theoryParams.KeyToWrap != unwrappedKey");

                theoryParams.EE.ProcessNoException();
            }
            catch (Exception ex)
            {
                theoryParams.EE.ProcessException(ex);
            }
        }

        public static TheoryData<RsaKeyWrapTestParams> RsaWrapUnwrapTheoryData()
        {
            var theoryData = new TheoryData<RsaKeyWrapTestParams>();

            // round trip positive tests
            AddWrapUnwrapTheoryData("Test1", SecurityAlgorithms.RsaPKCS1, KeyingMaterial.RsaSecurityKey1, theoryData);
            AddWrapUnwrapTheoryData("Test2", SecurityAlgorithms.RsaOAEP, KeyingMaterial.RsaSecurityKey1, theoryData);
            AddWrapUnwrapTheoryData("Test3", SecurityAlgorithms.RsaOAEP256, KeyingMaterial.RsaSecurityKey1, theoryData);

            // Wrap parameter checking
            AddWrapParameterCheckTheoryData("Test4", SecurityAlgorithms.Aes128KW, Default.SymmetricEncryptionKey128, null, ExpectedException.ArgumentNullException(), theoryData);
            byte[] keyToWrap = new byte[9];
            Array.Copy(Guid.NewGuid().ToByteArray(), keyToWrap, keyToWrap.Length);
            AddWrapParameterCheckTheoryData("Test4", SecurityAlgorithms.Aes128KW, Default.SymmetricEncryptionKey128, keyToWrap, ExpectedException.ArgumentException("IDX10664:"), theoryData);

            return theoryData;
        }

        private static void AddWrapUnwrapTheoryData(string testId, string algorithm, SecurityKey key, TheoryData<RsaKeyWrapTestParams> theoryData)
        {
            theoryData.Add(new RsaKeyWrapTestParams
            {
                EncryptAlgorithm = algorithm,
                KeyToWrap = Guid.NewGuid().ToByteArray(),
                EE = ExpectedException.NoExceptionExpected,
                EncryptKey = key,
                TestId = "AddWrapUnwrapTheoryData_" + testId
            });
        }

        private static void AddWrapParameterCheckTheoryData(string testId, string algorithm, SecurityKey key, byte[] keyToWrap, ExpectedException ee, TheoryData<RsaKeyWrapTestParams> theoryData)
        {
            theoryData.Add(new RsaKeyWrapTestParams
            {
                EncryptAlgorithm = algorithm,
                EncryptKey = key,
                KeyToWrap = keyToWrap,
                EE = ee,
                TestId = testId
            });
        }

        public class RsaKeyWrapTestParams
        {
            public string DecryptAlgorithm { get; set; }
            public SecurityKey DecryptKey { get; set; }
            public string EncryptAlgorithm { get; set; }
            public ExpectedException EE { get; set; }
            public SecurityKey EncryptKey { get; set; }
            public byte[] KeyToWrap { get; set; }
            public byte[] WrappedKey { get; set; }
            public KeyWrapProvider Provider { get; set; }
            public string TestId { get; set; }
        }
    }
}