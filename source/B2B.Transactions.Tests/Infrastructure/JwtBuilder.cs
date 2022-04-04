// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace B2B.Transactions.Tests.Infrastructure
{
    public class JwtBuilder
    {
        private static readonly JwtSecurityTokenHandler _tokenHandler = new();
        private static readonly byte[] _defaultSigningKey = Guid.NewGuid().ToByteArray();
        private static readonly DateTime _unixEpoc = new DateTime(1970, 1, 1);
        private readonly SymmetricSecurityKey _signingKey;
        private readonly List<Claim> _claims;
        private DateTime? _expires;
        private string? _audience;
        private DateTime? _notBefore;
        private string? _issuer;

        public JwtBuilder(byte[] signingKey)
        {
            _signingKey = new SymmetricSecurityKey(signingKey);
            _claims = new List<Claim>(16);
        }

        public JwtBuilder()
            : this(_defaultSigningKey) { }

        /// <summary>
        /// The signing key used to sign the JWT token
        /// </summary>
        public SymmetricSecurityKey SigningKey => _signingKey;

        public static JwtSecurityToken Parse(string token, SymmetricSecurityKey signingKey)
        {
            _ = _tokenHandler.ValidateToken(token, new TokenValidationParameters { ValidateIssuerSigningKey = true, IssuerSigningKey = signingKey, ValidateIssuer = false, ValidateAudience = false, }, out SecurityToken jwtSecurityToken);
            return (JwtSecurityToken)jwtSecurityToken;
        }

        /// <summary>
        /// Set the expiry date for the token
        /// </summary>
        /// <param name="expires">Expiry date and time for the token</param>
        /// <returns>This builder instance</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="expires" /> <see cref="DateTime.Kind" /> is not <see cref="DateTimeKind.Utc" /></exception>
        public JwtBuilder Expires(DateTime expires)
        {
            if (expires.Kind != DateTimeKind.Utc) throw new ArgumentException("Expiry date must be UTC", nameof(expires));
            _expires = expires;
            return this;
        }

        /// <summary>
        /// Sets the identity of the user
        /// </summary>
        /// <param name="userIdentity">User identity</param>
        /// <returns>This builder instance</returns>
        /// <exception cref="ArgumentException">Exception</exception>
        public JwtBuilder Subject(string userIdentity)
        {
            if (string.IsNullOrEmpty(userIdentity)) throw new ArgumentException("User identity cannot be null or empty", nameof(userIdentity));

            WithClaim(JwtRegisteredClaimNames.Sub, userIdentity);
            return this;
        }

        /// <summary>
        /// Set the AUD for the JWT token
        /// </summary>
        /// <param name="audience"></param>
        /// <returns>This builder instance</returns>
        /// <exception cref="ArgumentException">if audience is null or empty</exception>
        public JwtBuilder Audience(string audience)
        {
            if (string.IsNullOrEmpty(audience))
            {
                throw new ArgumentException($"'{nameof(audience)}' cannot be null or empty.", nameof(audience));
            }

            _audience = audience;
            return this;
        }

        /// <summary>
        /// Set the time the token were issued
        /// </summary>
        /// <param name="issuedAt">Date of issue</param>
        /// <param name="expiresAt">Date of expiration</param>
        /// <returns>This builder instance</returns>
        /// <exception cref="ArgumentException">Thrown if DateTimeKind is not UTC</exception>
        public JwtBuilder IssuedAt(DateTime issuedAt, DateTime expiresAt)
        {
            if (issuedAt.Kind != DateTimeKind.Utc) throw new ArgumentException($"'{nameof(issuedAt)}' must be in UTC", nameof(issuedAt));
            return
                Expires(expiresAt)
                .WithClaim(
                    type: JwtRegisteredClaimNames.Iat,
                    value: issuedAt.Subtract(JwtBuilder._unixEpoc).TotalSeconds.ToString(DateTimeFormatInfo.InvariantInfo),
                    valueType: ClaimValueTypes.Integer64);
        }

        /// <summary>
        /// Set issued at, if no Expiry date is set, then a lifetime of 1 hour is used for expiry with respect to issuedAt
        /// </summary>
        /// <param name="issuedAt"></param>
        /// <returns>This builder instance</returns>
        public JwtBuilder IssuedAt(DateTime issuedAt)
            => IssuedAt(issuedAt, _expires ?? issuedAt.AddHours(1));

        /// <summary>
        /// Set the issuer of the token
        /// </summary>
        /// <param name="issuer"></param>
        /// <returns>This builder instance</returns>
        /// <exception cref="ArgumentException">issuer cannot be null or empty</exception>
        public JwtBuilder Issuer(string issuer)
        {
            if (string.IsNullOrEmpty(issuer))
            {
                throw new ArgumentException($"'{nameof(issuer)}' cannot be null or empty.", nameof(issuer));
            }

            _issuer = issuer;

            return this;
        }

        /// <summary>
        /// Set the NotBefore time for the token
        /// </summary>
        /// <param name="notBefore">Do not use before</param>
        /// <returns>This builder instance</returns>
        /// <exception cref="ArgumentException">If notBefore is not in UTC</exception>
        public JwtBuilder NotBefore(DateTime notBefore)
        {
            if (notBefore.Kind != DateTimeKind.Utc) throw new ArgumentException($"'{nameof(notBefore)}' must be in UTC", nameof(notBefore));
            _notBefore = notBefore;
            return this;
        }

        /// <summary>
        /// Add a claim to the token
        /// </summary>
        /// <param name="type">type of claim</param>
        /// <param name="value">value for claim</param>
        /// <param name="valueType">valueType for claim</param>
        /// <returns>This builder instance</returns>
        public JwtBuilder WithClaim(string type, string value, string? valueType = null)
            => RegisterClaim(new(type, value, valueType));

        /// <summary>
        /// Add a claim to the token
        /// </summary>
        /// <param name="claim">claim to add</param>
        /// <returns>This builder instance</returns>
        public JwtBuilder WithClaim(Claim claim)
            => RegisterClaim(claim);

        /// <summary>
        /// Add a claim of type 'role' to the token
        /// </summary>
        /// <param name="role">role to add</param>
        /// <returns>This builder instance</returns>
        public JwtBuilder WithRole(string role)
            => RegisterClaim(new("roles", role));

        /// <summary>
        /// Build the token from the configuration
        /// </summary>
        /// <returns>string with the token</returns>
        public string CreateToken(string securityAlgorithm = SecurityAlgorithms.HmacSha256Signature)
        {
            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                notBefore: _notBefore,
                expires: _expires,
                signingCredentials: new SigningCredentials(_signingKey, securityAlgorithm),
                claims: _claims);
            return _tokenHandler.WriteToken(token);
        }

        private JwtBuilder RegisterClaim(Claim claim)
        {
            _claims.Add(claim);
            return this;
        }
    }
}
