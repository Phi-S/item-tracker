using System.Data;
using DbUp.Engine;

namespace infrastructure.Database.Migrations;

public class Script_0004_CreatePricesTable : IScript 
{
    public string ProvideScript(Func<IDbCommand> dbCommandFactory)
    {
	    const string createPricesTable = """
	                                     CREATE TABLE price (
	                                         id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
	                                         item_id BIGINT NOT NULL,
	                                         steam_price_usd_cents BIGINT NULL DEFAULT NULL,
	                                         price_refresh_id BIGINT NOT NULL,
	                                         CONSTRAINT fk_price_price_refresh FOREIGN KEY (price_refresh_id) REFERENCES price_refresh(id) 
	                                             ON UPDATE NO ACTION 
	                                             ON DELETE CASCADE
	                                     );
	                                     """;
	    
	    return createPricesTable;
    }
}