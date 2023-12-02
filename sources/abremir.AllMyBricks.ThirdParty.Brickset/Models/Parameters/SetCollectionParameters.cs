﻿using Newtonsoft.Json.Linq;

namespace abremir.AllMyBricks.ThirdParty.Brickset.Models.Parameters
{
    public class SetCollectionParameters : ParameterUserHashSetId
    {
        public bool Own { get; set; }
        public bool Want { get; set; }
        public int QtyOwned { get; set; }
        public string Notes { get; set; }
        public int Rating { get; set; }

        public ParameterSetCollection ToParameterSetCollection()
        {
            return new ParameterSetCollection
            {
                ApiKey = ApiKey,
                SetID = SetID,
                UserHash = UserHash,
                Params = GetParams()
            };
        }

        private string GetParams()
        {
            dynamic @params = new JObject();

            @params.own = Own ? 1 : 0;
            @params.want = Want ? 1 : 0;
            @params.qtyOwned = QtyOwned;
            @params.notes = Notes?.Substring(0, 200).Trim();
            @params.rating = Rating;

            return @params.ToString();
        }
    }
}
