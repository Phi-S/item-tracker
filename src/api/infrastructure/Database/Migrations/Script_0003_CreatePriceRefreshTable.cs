using System.Data;
using DbUp.Engine;

namespace infrastructure.Database.Migrations;

public class Script_0003_CreatePriceRefreshTable : IScript 
{
    public string ProvideScript(Func<IDbCommand> dbCommandFactory)
    {
	    const string createPriceRefreshTable = """
	                                           CREATE TABLE price_refresh (
	                                               id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
	                                               usd_to_eur_exchange_rate DOUBLE PRECISION NOT NULL,
	                                               steam_prices_last_modified TIMESTAMPTZ NOT NULL,
	                                               created_at BIGINT NOT NULL
	                                           );
	                                           
	                                           """;
	    
	    return createPriceRefreshTable;
    }
}