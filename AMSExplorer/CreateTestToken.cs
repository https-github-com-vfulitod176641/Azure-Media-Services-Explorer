﻿//----------------------------------------------------------------------- 
// <copyright file="CreateLocator.cs" company="Microsoft">Copyright (c) Microsoft Corporation. All rights reserved.</copyright> 
// <license>
// Azure Media Services Explorer Ver. 3.1
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
//  
// http://www.apache.org/licenses/LICENSE-2.0 
//  
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License. 
// </license> 

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.IdentityModel.Tokens;
using Microsoft.WindowsAzure.MediaServices.Client.ContentKeyAuthorization;
using Microsoft.WindowsAzure.MediaServices.Client.DynamicEncryption;

namespace AMSExplorer
{
    public partial class CreateTestToken : Form
    {
        private IContentKeyAuthorizationPolicy _policy;
        private BindingList<MyTokenClaim> TokenClaimsList = new BindingList<MyTokenClaim>();
        private X509Certificate2 cert = null;


        public DateTime? StartDate
        {
            get
            {
                return (checkBoxStartDate.Checked) ? (DateTime)dateTimePickerStartDate.Value.ToUniversalTime() : (Nullable<DateTime>)null;
            }
            set
            {
                dateTimePickerStartDate.Value = (DateTime)value;
                dateTimePickerStartTime.Value = (DateTime)value;
            }
        }

       

        public bool PutContentKeyIdentifier
        {
            get { return checkBoxAddContentKeyIdentifierClaim.Checked; }
        }

        public string GetAudienceUri
        {
            get
            {
                return textBoxAudience.Text;
            }
        }
        public string GetIssuerUri
        {
            get
            {
                return textBoxIssuer.Text;
            }
        }


        public DateTime? EndDate
        {
            get
            {
                return (checkBoxEndDate.Checked) ? (DateTime)dateTimePickerEndDate.Value.ToUniversalTime() : (Nullable<DateTime>)null;
            }
            set
            {
                dateTimePickerEndDate.Value = (DateTime)value;
                dateTimePickerEndTime.Value = (DateTime)value;
            }
        }

        


        public IContentKeyAuthorizationPolicyOption GetOption
        {
            get
            {
                if (listViewAutOptions.SelectedIndices.Count > 0)
                {
                    return _policy.Options.Skip(listViewAutOptions.SelectedIndices[0]).Take(1).FirstOrDefault();
                }
                else
                {
                    return null;
                }
            }
        }

        public IList<Claim> GetTokenRequiredClaims
        {
            get
            {
                IList<Claim> mylist = new List<Claim>();
                foreach (var j in TokenClaimsList)
                {
                    if (!string.IsNullOrEmpty(j.Type))
                    {
                        mylist.Add(new Claim(j.Type, j.Value));
                    }
                }
                return mylist;
            }
        }

        public IList<TokenClaim> GetTokenRequiredTokenClaims
        {
            get
            {
                IList<TokenClaim> mylist = new List<TokenClaim>();
                foreach (var j in TokenClaimsList)
                {
                    if (!string.IsNullOrEmpty(j.Type))
                    {
                        mylist.Add(new TokenClaim(j.Type, j.Value));
                    }
                }
                return mylist;
            }
        }




        public CreateTestToken(IAsset MyAsset, ContentKeyType keytype, CloudMediaContext _context, IContentKeyAuthorizationPolicy policy, SigningCredentials signingcredentials = null, string optionid = null)
        {
            InitializeComponent();
            this.Icon = Bitmaps.Azure_Explorer_ico;

            _policy = policy;

            var query = _policy.Options;
            listViewAutOptions.BeginUpdate();
            listViewAutOptions.Items.Clear();
            foreach (var option in query)
            {
                ListViewItem item = new ListViewItem(option.Name, 0);
                item.SubItems.Add(option.Id);

                string tokenTemplateString = option.Restrictions.FirstOrDefault().Requirements;
                if (!string.IsNullOrEmpty(tokenTemplateString))
                {
                    TokenRestrictionTemplate tokenTemplate = TokenRestrictionTemplateSerializer.Deserialize(tokenTemplateString);
                    item.SubItems.Add(tokenTemplate.TokenType == TokenType.JWT ? "JWT" : "SWT");
                    item.SubItems.Add(tokenTemplate.PrimaryVerificationKey.GetType() == typeof(SymmetricVerificationKey) ? "Symmetric" : "Asymmetric X509");
                }
                listViewAutOptions.Items.Add(item);
            }
            listViewAutOptions.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listViewAutOptions.EndUpdate();


            /*
            string tokenTemplateString = option.Restrictions.FirstOrDefault().Requirements;
            if (!string.IsNullOrEmpty(tokenTemplateString))
            {
                Guid rawkey = EncryptionUtils.GetKeyIdAsGuid(key.Id);
                TokenRestrictionTemplate tokenTemplate = TokenRestrictionTemplateSerializer.Deserialize(tokenTemplateString);

                if (tokenTemplate.TokenType == TokenType.SWT) //SWT
                {
                    testToken = TokenRestrictionTemplateSerializer.GenerateTestToken(tokenTemplate, null, rawkey, DateTime.Now.AddMinutes(Properties.Settings.Default.DefaultTokenDuration));
                }
                else // JWT
                {
                    List<Claim> myclaims = null;
                    myclaims = new List<Claim>();
                    myclaims.Add(new Claim(TokenClaim.ContentKeyIdentifierClaimType, rawkey.ToString()));

                    if (tokenTemplate.PrimaryVerificationKey.GetType() == typeof(SymmetricVerificationKey))
                    {
                        InMemorySymmetricSecurityKey tokenSigningKey = new InMemorySymmetricSecurityKey((tokenTemplate.PrimaryVerificationKey as SymmetricVerificationKey).KeyValue);
                        signingcredentials = new SigningCredentials(tokenSigningKey, SecurityAlgorithms.HmacSha256Signature, SecurityAlgorithms.Sha256Digest);
                    }
                    else if (tokenTemplate.PrimaryVerificationKey.GetType() == typeof(X509CertTokenVerificationKey))
                    {
                        if (signingcredentials == null)
                        {
                            X509Certificate2 cert = DynamicEncryption.GetCertificateFromFile(true);
                            if (cert != null) signingcredentials = new X509SigningCredentials(cert);
                        }
                    }
                    JwtSecurityToken token = new JwtSecurityToken(issuer: tokenTemplate.Issuer.AbsoluteUri, audience: tokenTemplate.Audience.AbsoluteUri, notBefore: DateTime.Now.AddMinutes(-5), expires: DateTime.Now.AddMinutes(Properties.Settings.Default.DefaultTokenDuration), signingCredentials: signingcredentials, claims: myclaims);
                    JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                    testToken = handler.WriteToken(token);
                }
            }
             * */


        }



        private void dateTimePickerStartDate_ValueChanged(object sender, EventArgs e)
        {
            dateTimePickerStartTime.Value = dateTimePickerStartDate.Value;
        }

        private void dateTimePickerStartTime_ValueChanged(object sender, EventArgs e)
        {
            dateTimePickerStartDate.Value = dateTimePickerStartTime.Value;
        }

        private void dateTimePickerEndDate_ValueChanged(object sender, EventArgs e)
        {
            dateTimePickerEndTime.Value = dateTimePickerEndDate.Value;
        }

        private void dateTimePickerEndTime_ValueChanged(object sender, EventArgs e)
        {
            dateTimePickerEndDate.Value = dateTimePickerEndTime.Value;
        }

        private void checkBoxStartDate_CheckedChanged_1(object sender, EventArgs e)
        {
            dateTimePickerStartDate.Enabled = dateTimePickerStartTime.Enabled = checkBoxStartDate.Checked;
        }

        private void CreateTestToken_Load(object sender, EventArgs e)
        {
            dataGridViewTokenClaims.DataSource = TokenClaimsList;

        }

       

        private void listViewAutOptions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewAutOptions.SelectedIndices.Count > 0)
            {
                string tokenTemplateString = _policy.Options.Skip(listViewAutOptions.SelectedIndices[0]).Take(1).FirstOrDefault().Restrictions.FirstOrDefault().Requirements;
                if (!string.IsNullOrEmpty(tokenTemplateString))
                {
                    TokenRestrictionTemplate tokenTemplate = TokenRestrictionTemplateSerializer.Deserialize(tokenTemplateString);
                    textBoxAudience.Text = tokenTemplate.Audience.ToString();
                    textBoxIssuer.Text = tokenTemplate.Issuer.ToString();
                    checkBoxAddContentKeyIdentifierClaim.Checked = false;
                    groupBoxStartDate.Enabled = (tokenTemplate.TokenType == TokenType.JWT);
                    panelJWTX509Cert.Enabled = !(tokenTemplate.PrimaryVerificationKey.GetType() == typeof(SymmetricVerificationKey));
                    TokenClaimsList.Clear();
                    foreach (var claim in tokenTemplate.RequiredClaims)
                    {
                        if (claim.ClaimType == TokenClaim.ContentKeyIdentifierClaimType)
                        {
                            checkBoxAddContentKeyIdentifierClaim.Checked = true;
                        }
                        else
                        {
                            TokenClaimsList.Add(new MyTokenClaim()
                            {
                                Type = claim.ClaimType,
                                Value = claim.ClaimValue
                            });
                        }
                    }
                }

            }

        }

        private void buttonDelClaim_Click(object sender, EventArgs e)
        {
            if (dataGridViewTokenClaims.SelectedRows.Count == 1)
            {
                TokenClaimsList.RemoveAt(dataGridViewTokenClaims.SelectedRows[0].Index);
            }
        }

        private void buttonAddClaim_Click(object sender, EventArgs e)
        {
            TokenClaimsList.AddNew();
        }

        private void checkBoxEndDate_CheckedChanged(object sender, EventArgs e)
        {
            dateTimePickerEndDate.Enabled = dateTimePickerEndTime.Enabled = checkBoxEndDate.Checked;
        }


    }
}