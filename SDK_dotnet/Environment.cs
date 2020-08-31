﻿using Gluwa.SDK_dotnet.Utils;

namespace Gluwa.SDK_dotnet
{
    public class Environment
    {
        public static Environment Production
        {
            get
            {
                Environment env = new Environment(
                    baseUrl: Constants.GLUWA_API_BASE_URL,
                    usdgContractAddress: Constants.GLUWACOIN_USDG_CONTRACT_ADDRESS,
                    krwgContractAddress: Constants.GLUWACOIN_KRWG_CONTRACT_ADDRESS,
                    ngngContractAddress: Constants.GLUWACOIN_NGNG_CONTRACT_ADDRESS);

                return env;
            }
        }

        public static Environment Sandbox
        {
            get
            {
                Environment env = new Environment(
                    baseUrl: Constants.GLUWA_SANDBOX_API_BASE_URL,
                    usdgContractAddress: Constants.GLUWACOIN_SANDBOX_USDG_CONTRACT_ADDRESS,
                    krwgContractAddress: Constants.GLUWACOIN_SANDBOX_KRWG_CONTRACT_ADDRESS,
                    ngngContractAddress: Constants.GLUWACOIN_SANDBOX_NGNG_CONTRACT_ADDRESS);

                return env;
            }
        }

        public string BaseUrl { get; }

        public string UsdgContractAddress { get; }

        public string KrwgContractAddress { get; }

        public string NgngContractAddress { get; }

        protected Environment (
            string baseUrl,
            string usdgContractAddress,
            string krwgContractAddress,
            string ngngContractAddress)
        {
            BaseUrl = baseUrl;
            UsdgContractAddress = usdgContractAddress;
            KrwgContractAddress = krwgContractAddress;
            NgngContractAddress = ngngContractAddress;
        }
    }
}