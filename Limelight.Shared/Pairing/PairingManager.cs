﻿namespace Limelight
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.Security.Cryptography.Certificates;
    using Windows.Security.Cryptography.Core;
    using Windows.UI.Popups;
    using Windows.UI.Xaml.Controls;
    using Windows.Web.Http.Filters;

    using Limelight.Streaming;

    /// <summary>
    /// Performs pairing with the streaming machine
    /// </summary>
    public class PairingManager
    {
        private NvHttp nv;

        /// <summary>
        /// Constructor that sets nv 
        /// </summary>
        /// <param name="nv">The NvHttp Object</param>
        public PairingManager(Computer computer)
        {
            this.nv = new NvHttp(computer.IpAddress); 
        }
        #region Pairing
        /// <summary>
        /// Pair with the hostname in the textbox
        /// </summary>
        public async Task Pair(Computer c)
        {
            Debug.WriteLine("Pairing...");

            // Get the pair state.
            bool? pairState = await QueryPairState(); 
            if (pairState == true)
            {
                var dialog = new MessageDialog("This device is already paired to the host PC", "Already Paired");
                dialog.ShowAsync();
                Debug.WriteLine("Already paired");
                return;
            }
            // pairstate = null. We've encountered an error
            else if (!pairState.HasValue)
            {
                var dialog = new MessageDialog("Failed to query pair state", "Pairing failed");
                dialog.ShowAsync();
                Debug.WriteLine("Query pair state failed");
                return;
            }

            bool challenge = await PairingCryptoHelpers.PerformPairingHandshake(new WPCryptoProvider(), nv, nv.GetUniqueId());
            if (!challenge)
            {
                Debug.WriteLine("Challenges failed");
                return; 
            } 

            // Otherwise, everything was successful
            MainPage.SaveComputer(c);
            var successDialog = new MessageDialog("Pairing successful", "Success");
            await successDialog.ShowAsync();
        }

        #endregion Pairing

        #region XML Queries
        /// <summary>
        /// Query the server to get the device pair state
        /// </summary>
        /// <returns>True if device is already paired, false if not, null if failure</returns>
        public async Task<bool?> QueryPairState()
        {
            XmlQuery pairState;
            try
            {
                pairState = new XmlQuery(nv.BaseUrl + "/serverinfo?uniqueid=" + nv.GetUniqueId());
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to get pair state: " + e.Message);
                return null;
            }

            // Check if the device is paired by checking the XML attribute within the <paired> tag
            if (String.Compare(pairState.XmlAttribute("PairStatus"), "1") != 0)
            {
                Debug.WriteLine("Not paired");
                return false;
            }

            // We're already paired if we get here!
            return true;
        }

        #endregion XML Queries
    }
}