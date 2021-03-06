﻿using Gluwa.SDK_dotnet.Error;
using Gluwa.SDK_dotnet.Models;
using Gluwa.SDK_dotnet.Utils;
using Nethereum.Signer;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Gluwa.SDK_dotnet.Clients
{
    /// <summary>
    /// QRCodeClient generates payment QR code image.
    /// </summary>
    public sealed class QRCodeClient
    {
        private Environment mEnv;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="bSandbox">Set to 'true' if using sandbox mode. Otherwise, 'false'</param>
        public QRCodeClient(
            bool bSandbox = false)
        {
            if (bSandbox)
            {
                mEnv = Environment.Sandbox;
            }
            else
            {
                mEnv = Environment.Production;
            }
        }

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="env"></param>
        public QRCodeClient(Environment env)
        {
            mEnv = env;
        }

        /// <summary>
        /// Generates a one-time use QR code for merchants, used for making a payment transaction. Returns an image in a .jpg or .png format.
        /// </summary>
        /// <param name="apiKey">Your API Key.</param>
        /// <param name="secret">Your API Secret.</param>
        /// <param name="address">Your public address.</param>
        /// <param name="privateKey">Your private Key.</param>
        /// <param name="currency">Currency type.</param>
        /// <param name="amount">Payment amount. Fee will be deducted from this amount when payment request is made.</param>
        /// <param name="format">Desired image format, optional. Defaults to base64 string</param>
        /// <param name="note">Additional information, used by the merchant user. optional.</param>
        /// <param name="merchantOrderID">Identifier for the payment, used by the merchant user. optional.</param>
        /// <param name="expiry">Time of expiry for the QR code in seconds. Payment request must be made with this QR code before this time. optional. Defaults to 1800</param>
        /// <response code="200">QR code image in a .png by default or .jpg depending on the format query parameter.</response> 
        /// <response code="400">Validation error. Please see inner errors for more details. or API Key and secret request header is missing or invalid.</response>        
        /// <response code="403">Combination of Api Key and Api Secret was not found.</response>        
        /// <response code="500">Server error.</response>
        /// <response code="503">Service unavailable for the provided currency.</response>
        public async Task<Result<string, ErrorResponse>> GetPaymentQRCodeAsync(
            string apiKey,
            string secret,
            string address,
            string privateKey,
            EPaymentCurrency currency,
            string amount,
            string format = null,
            string note = null,
            string merchantOrderID = null,
            int expiry = 1800
            )
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey));
            }
            else if (string.IsNullOrWhiteSpace(secret))
            {
                throw new ArgumentNullException(nameof(secret));
            }
            else if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentNullException(nameof(address));
            }
            else if (string.IsNullOrWhiteSpace(privateKey))
            {
                throw new ArgumentNullException(nameof(privateKey));
            }
            else if (string.IsNullOrWhiteSpace(amount))
            {
                throw new ArgumentNullException(nameof(amount));
            }

            var result = new Result<string, ErrorResponse>();
            var requestUri = $"{mEnv.BaseUrl}/v1/QRCode";

            var queryParams = new List<string>();
            if (format != null)
            {
                queryParams.Add($"format={format}");
                requestUri = $"{requestUri}?{string.Join("&", queryParams)}";
            }

            QRCodeRequest bodyParams = new QRCodeRequest()
            {
                Signature = getTimestampSignature(privateKey),
                Currency = currency,
                Target = address,
                Amount = amount,
                Expiry = expiry,
                Note = note,
                MerchantOrderID = merchantOrderID
            };

            string json = bodyParams.ToJson();
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            byte[] authenticationBytes = Encoding.ASCII.GetBytes($"{apiKey}:{secret}");
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
                         System.Convert.ToBase64String(authenticationBytes));
                    using (HttpResponseMessage response = await httpClient.PostAsync(requestUri, content))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            result.IsSuccess = true;
                            result.Data = await response.Content.ReadAsStringAsync();

                            return result;
                        }

                        string contentString = await response.Content.ReadAsStringAsync();
                        result.Error = ResponseHandler.GetError(response.StatusCode, requestUri, contentString);
                    }
                }
            }
            catch (HttpRequestException)
            {
                result.IsSuccess = false;
                result.Error = ResponseHandler.GetExceptionError();
            }

            return result;
        }

        private string getTimestampSignature(string privateKey)
        {
            var signer = new EthereumMessageSigner();
            string Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString();
            string signature = signer.EncodeUTF8AndSign(Timestamp, new EthECKey(privateKey));

            string gluwaSignature = $"{Timestamp}.{signature}";
            byte[] gluwaSignatureByte = Encoding.UTF8.GetBytes(gluwaSignature);
            string encodedData = System.Convert.ToBase64String(gluwaSignatureByte);

            return encodedData;
        }
    }
}