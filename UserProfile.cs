// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NewDialogBot
{
    public class UserProfile
    {
        //public string Name { get; set; }
        public string Email { get; set; }
        public long Phone { get; set; }
        public long Choice { get; set; }
        public string AuthorizationSecretToken { get; set; }
        public string Given { get; set; }
        public string Family { get; set; }
        public string Name { get; set; }
        public string BirthDate { get; set; }
        public string Gender { get; set; }
        public int AddressPostalcode { get; set; }
    }

}
