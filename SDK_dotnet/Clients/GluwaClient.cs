﻿using Gluwa.SDK_dotnet.Error;
using Gluwa.SDK_dotnet.Models;
using Gluwa.SDK_dotnet.Utils;
using NBitcoin;
using Nethereum.ABI;
using Nethereum.Signer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Gluwa.SDK_dotnet.Clients
{
    /// <summary>
    /// Client for public APIs
    /// </summary>
    public sealed class GluwaClient
    {
        private readonly Environment mEnv;
        private readonly string X_REQUEST_SIGNATURE = "X-REQUEST-SIGNATURE";

        private const int MAX_UNSPENTOUTPUTS_COUNT = 5;

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="bTest">Set to 'true' if using test mode. Otherwise, 'false'</param>
        public GluwaClient(
            bool bTest = false)
        {
            if (bTest)
            {
                mEnv = Environment.Test;
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
        public GluwaClient(Environment env)
        {
            mEnv = env;
        }

        /// <summary>
        /// Get balance for specified currency.
        /// </summary>
        /// <param name="currency">Currency type.</param>
        /// <param name="address">Your public Address.</param>
        /// <param name="includeUnspentOutputs">(For BTC only) if "true", the response includes unspent outputs for the address. "false" by default.</param>
        /// <response code="200">Balance and associated currency.</response>
        /// <response code="400">Invalid address format.</response>
        /// <response code="500">Server error.</response>
        /// <response code="503">Service unavailable for the specified currency or temporarily.</response>
        public async Task<Result<BalanceResponse, ErrorResponse>> GetBalanceAsync(ECurrency currency, string address, bool includeUnspentOutputs = false)
        {
            validateParam(address);

            var result = new Result<BalanceResponse, ErrorResponse>();
            string requestUri = $"{mEnv.BaseUrl}/v1/{currency}/Addresses/{address}";

            List<string> queryParams = new List<string>();
            queryParams.Add($"includeUnspentOutputs={includeUnspentOutputs}");

            if (queryParams.Any())
            {
                requestUri = $"{requestUri}?{string.Join("&", queryParams)}";
            }

            try
            {
                using (HttpClient httpClient = new HttpClient())
                using (HttpResponseMessage response = await httpClient.GetAsync(requestUri))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        BalanceResponse balanceResponse = await response.Content.ReadAsAsync<BalanceResponse>();
                        result.IsSuccess = true;
                        result.Data = balanceResponse;

                        return result;
                    }

                    string contentString = await response.Content.ReadAsStringAsync();
                    result.Error = ResponseHandler.GetError(response.StatusCode, requestUri, contentString);
                }
            }
            catch (HttpRequestException)
            {
                result.IsSuccess = false;
                result.Error = ResponseHandler.GetExceptionError();
            }

            return result;
        }

        /// <summary>
        /// Get a list of transactions for specified currency.
        /// </summary>
        /// <param name="currency">Currency type.</param>
        /// <param name="address">Your public Address.</param>
        /// <param name="privateKey">Your Private Key.</param>
        /// <param name="limit">Number of transactions to include in the result. optional. Defaults to 100.</param>
        /// <param name="status">Filter by transaction status. Optional. Defaults to Confimred.</param>
        /// <param name="offset">Number of transactions to skip; used for pagination. Optional. Default to 0.</param>
        /// <response code="200">List of transactions associated with the address.</response>
        /// <response code="400">Invalid request or Address does not have a valid format.</response>
        /// <response code="403">Request signature header is not valid.</response>
        /// <response code="500">Server error.</response>
        /// <response code="503">Service unavailable.</response>
        public async Task<Result<List<TransactionResponse>, ErrorResponse>> GetTransactionListAsync(
           ECurrency currency,
           string address,
           string privateKey,
           uint limit = 100,
           ETransactionStatusFilter status = ETransactionStatusFilter.Confirmed,
           uint offset = 0)
        {
            validateParam(address);

            validateParam(privateKey);

            var result = new Result<List<TransactionResponse>, ErrorResponse>();
            string requestUri = $"{mEnv.BaseUrl}/v1/{currency}/Addresses/{address}/Transactions";

            var queryParams = new List<string>();
            if (offset > 0)
            {
                queryParams.Add($"offset={offset}");
            }
            if (limit > 0)
            {
                queryParams.Add($"limit={limit}");
            }
            if (queryParams.Any())
            {
                queryParams.Add($"status={status}");
                requestUri = $"{requestUri}?{string.Join("&", queryParams)}";
            }

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add(X_REQUEST_SIGNATURE, GluwaService.GetAddressSignature(privateKey, currency, mEnv));

                    using (HttpResponseMessage response = await httpClient.GetAsync(requestUri))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            List<TransactionResponse> transactionResponse = await response.Content.ReadAsAsync<List<TransactionResponse>>();
                            result.IsSuccess = true;
                            result.Data = transactionResponse;

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

        /// <summary>
        /// Get bitcoin or gluwacoin transaction by hash.
        /// </summary>
        /// <param name="currency">Currency type</param>
        /// <param name="privateKey">Your Private Key.</param>
        /// <param name="txnHash">Hash of the transaction on the blockchain.</param>
        /// <response code="200">Transaction response.</response>
        /// <response code="400">Invalid transaction hash format.</response>
        /// <response code="403">Request signature header is not valid.</response>
        /// <response code="404">Tranasction not found.</response>
        /// <response code="500">Server error.</response>
        /// <response code="503">Service unavailable.</response>
        public async Task<Result<TransactionResponse, ErrorResponse>> GetTransactionDetailsAsync(ECurrency currency, string privateKey, string txnHash)
        {
            validateParam(privateKey);

            validateParam(txnHash);

            var result = new Result<TransactionResponse, ErrorResponse>();
            string requestUri = $"{mEnv.BaseUrl}/v1/{currency}/Transactions/{txnHash}";

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add(X_REQUEST_SIGNATURE, GluwaService.GetAddressSignature(privateKey, currency, mEnv));

                    using (HttpResponseMessage response = await httpClient.GetAsync(requestUri))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            TransactionResponse transactionResponse = await response.Content.ReadAsAsync<TransactionResponse>();
                            result.IsSuccess = true;
                            result.Data = transactionResponse;

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

        /// <summary>
        /// Create a new Bitcoin or Gluwacoin transaction.
        /// </summary>
        /// <param name="currency">Currency type</param>
        /// <param name="address">Your public Address.</param>
        /// <param name="privateKey">Your Private Key.</param>
        /// <param name="amount">Transaction amount, not including the fee.</param>
        /// <param name="target">The address that the transaction will be sent to.</param>
        /// <param name="merchantOrderID">Identifier for the transaction that was provided by the merchant user. Optional.</param>
        /// <param name="note">Additional information about the transaction that a user can provide. Optional.</param>
        /// <param name="nonce">Nonce for the transaction. For Gluwacoin currencies only.</param>
        /// <param name="idem">Idempotent key for the transaction to prevent duplicate transactions.</param>
        /// <param name="paymentID">ID for the QR code payment.</param>
        /// <param name="paymentSig">Signature of the QR code payment.Required if PaymentID is not null.</param>
        /// <response code="202">Newly accepted transaction.</response>
        /// <response code="400">Invalid request. or Validation error. See inner errors for more details. or (BTC only) Signed BTC transaction could not be verified.</response>
        /// <response code="403">For payments, payment signature could not be verified.</response>
        /// <response code="409">A transaction with the same transaction hash, payment ID, or idem already exists.</response>
        /// <response code="500">Server error.</response>
        /// <response code="503">Service unavailable.</response>
        public async Task<Result<bool, ErrorResponse>> CreateTransactionAsync(CreateTransactionRequest request)
        {
            validateParams(request);

            var result = new Result<bool, ErrorResponse>();
            var requestUri = $"{mEnv.BaseUrl}/v1/Transactions";

            Result<FeeResponse, ErrorResponse> getFee = await getFeeAsync(request.Currency, request.Amount);
            if (getFee.IsFailure)
            {
                result.Error = getFee.Error;

                return result;
            }

            string fee = getFee.Data.MinimumFee;
            string signature = null;

            if (request.Currency == ECurrency.BTC)
            {
                signature = await getBtcTransactionSignatureAsync(request.Currency, request.Address, request.Amount, fee, request.Target, request.PrivateKey);
            }
            else
            {
                if (request.Nonce == null)
                {
                    request.Nonce = GluwaService.GetNonceString();
                }

                signature = getGluwacoinTransactionSignature(request.Currency, request.Amount, fee, request.Nonce, request.Address, request.Target, request.PrivateKey);
            }

            TransactionRequest bodyParams = new TransactionRequest
            {
                Signature = signature,
                Currency = request.Currency,
                Target = request.Target,
                Amount = request.Amount,
                Fee = getFee.Data.MinimumFee,
                Source = request.Address,
                Nonce = request.Nonce,
                MerchantOrderID = request.MerchantOrderID,
                Note = request.Note,
                Idem = request.Idem,
                PaymentID = request.PaymentID,
                PaymentSig = request.PaymentSig
            };

            string json = bodyParams.ToJson();
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                using (HttpClient httpClient = new HttpClient())
                using (var response = await httpClient.PostAsync(requestUri, content))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        result.IsSuccess = true;
                        result.Data = true;

                        return result;
                    }

                    string contentString = await response.Content.ReadAsStringAsync();
                    result.Error = ResponseHandler.GetError(response.StatusCode, requestUri, contentString);
                }
            }
            catch (HttpRequestException)
            {
                result.IsSuccess = false;
                result.Error = ResponseHandler.GetExceptionError();
            }
            return result;
        }

        private void validateParam(string param)
        {
            if (string.IsNullOrWhiteSpace(param))
            {
                throw new ArgumentException(nameof(param));
            }
        }

        private void validateParams(CreateTransactionRequest request)
        {
            validateParam(request.Address);

            validateParam(request.PrivateKey);

            validateParam(request.Amount);

            validateParam(request.Target);

            if (request.PaymentID != null && string.IsNullOrWhiteSpace(request.PaymentSig))
            {
                throw new ArgumentException(nameof(request.PaymentSig));
            }
        }

        private string getGluwacoinTransactionSignature(ECurrency currency, string amount, string fee, string nonce, string address, string target, string privateKey)
        {
            BigInteger convertAmount = BigInteger.Zero;
            BigInteger convertFee = BigInteger.Zero;

            if (currency.IsGluwacoinSideChainCurrency())
            {
                convertAmount += GluwacoinConverter.ConvertToGluwacoinSideChainBigInteger(amount, currency);
                convertFee += GluwacoinConverter.ConvertToGluwacoinSideChainBigInteger(fee.ToString(), currency);
            }
            else
            {
                convertAmount += GluwacoinConverter.ConvertToGluwacoinBigInteger(amount);
                convertFee += GluwacoinConverter.ConvertToGluwacoinBigInteger(fee.ToString());
            }

            ABIEncode abiEncode = new ABIEncode();
            byte[] messageHash;

            var chainID = mEnv == Environment.Test ? 5 : 1; // 5 is Goerli Testnet | 1 is Mainnet

            // USDCG and sSGDG have different signature requirements
            if (currency == ECurrency.USDCG)
            {
                messageHash = abiEncode.GetSha3ABIEncodedPacked(
                    new ABIValue("uint8", 4),// Domain 4 is for transfer
                    new ABIValue("uint256", chainID),
                    new ABIValue("address", GluwaService.getGluwacoinContractAddress(currency, mEnv)),
                    new ABIValue("address", address),
                    new ABIValue("address", target),
                    new ABIValue("uint256", convertAmount),
                    new ABIValue("uint256", convertFee),
                    new ABIValue("uint256", BigInteger.Parse(nonce))
                );
            }
            else
            {
                messageHash = abiEncode.GetSha3ABIEncodedPacked(
                    new ABIValue("address", GluwaService.getGluwacoinContractAddress(currency, mEnv)),
                    new ABIValue("address", address),
                    new ABIValue("address", target),
                    new ABIValue("uint256", convertAmount),
                    new ABIValue("uint256", convertFee),
                    new ABIValue("uint256", BigInteger.Parse(nonce))
                );
            }

            EthereumMessageSigner signer = new EthereumMessageSigner();
            string signature = signer.Sign(messageHash, privateKey);

            return signature;
        }

        private async Task<string> getBtcTransactionSignatureAsync(ECurrency currency, string address, string amount, string fee, string target, string privateKey)
        {
            Result<BalanceResponse, ErrorResponse> getUnspentOutput = await GetBalanceAsync(currency, address, true);
            List<UnspentOutput> unspentOutputs = getUnspentOutput.Data.UnspentOutputs.OrderByDescending(u => u.Amount).ToList();

            Money amountValue = Money.Parse(amount);
            Money feeValue = Money.Parse(fee);
            Money totalAmountAndFeeValue = amountValue + feeValue;
            BigInteger totalAmountAndFee = new BigInteger(totalAmountAndFeeValue.Satoshi);

            BitcoinAddress sourceAddress = BitcoinAddress.Create(address, mEnv.Network);
            BitcoinAddress targetAddress = BitcoinAddress.Create(target, mEnv.Network);
            BitcoinSecret secret = new BitcoinSecret(privateKey, mEnv.Network);

            List<UnspentOutput> usingUnspentOutputs = new List<UnspentOutput>();
            BigInteger unspentOutputTotalAmount = BigInteger.Zero;
            for (int i = 0; i < unspentOutputs.Count; i++)
            {
                if (unspentOutputTotalAmount < totalAmountAndFee && i >= MAX_UNSPENTOUTPUTS_COUNT)
                {
                    throw new InvalidOperationException($"Could not find up to {MAX_UNSPENTOUTPUTS_COUNT} BTC unspent outputs that can cover the amount and fee.");
                }

                if (unspentOutputTotalAmount >= totalAmountAndFee)
                {
                    break;
                }

                usingUnspentOutputs.Add(unspentOutputs[i]);
                Money sumAmount = Money.Parse(unspentOutputs[i].Amount);
                unspentOutputTotalAmount += new BigInteger(sumAmount.Satoshi);
            }

            List<Coin> coins = new List<Coin>();
            for (int i = 0; i < usingUnspentOutputs.Count; i++)
            {
                coins.Add(new Coin(
                    fromTxHash: new uint256(usingUnspentOutputs[i].TxHash),
                    fromOutputIndex: (uint)usingUnspentOutputs[i].Index,
                    amount: usingUnspentOutputs[i].Amount,
                    scriptPubKey: Script.FromHex(sourceAddress.ScriptPubKey.ToHex())
                ));
            }

            TransactionBuilder builder = mEnv.Network.CreateTransactionBuilder();
            NBitcoin.Transaction txn = builder
                            .AddKeys(secret)
                            .AddCoins(coins)
                            .Send(targetAddress, amount)
                            .SetChange(sourceAddress)
                            .SendFees(fee)
                            .BuildTransaction(true);

            if (!builder.Verify(txn, out NBitcoin.Policy.TransactionPolicyError[] error))
            {
                throw new InvalidOperationException(string.Join(System.Environment.NewLine, error.Select(e => e.ToString())));
            }

            string signature = txn.ToHex();

            return signature;
        }

        private async Task<Result<FeeResponse, ErrorResponse>> getFeeAsync(ECurrency currency, string amount)
        {
            var result = new Result<FeeResponse, ErrorResponse>();
            string requestUri = $"{mEnv.BaseUrl}/v1/{currency}/Fee?amount={amount}";

            try
            {
                using (HttpClient httpClient = new HttpClient())
                using (HttpResponseMessage response = await httpClient.GetAsync(requestUri))
                {
                    FeeResponse feeResponse = await response.Content.ReadAsAsync<FeeResponse>();

                    if (response.IsSuccessStatusCode)
                    {
                        result.IsSuccess = true;
                        result.Data = feeResponse;

                        return result;
                    }

                    string contentString = await response.Content.ReadAsStringAsync();
                    result.Error = ResponseHandler.GetError(response.StatusCode, requestUri, contentString);
                }
            }
            catch (HttpRequestException)
            {
                result.IsSuccess = false;
                result.Error = ResponseHandler.GetExceptionError();
            }
            return result;
        }
    }
}
