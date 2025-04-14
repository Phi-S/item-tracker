using System.Data;
using DbUp.Engine;

namespace infrastructure.Database.Migrations;

public class Script_0002_CreateItemActionsTable : IScript 
{
    public string ProvideScript(Func<IDbCommand> dbCommandFactory)
    {
	    const string createItemActionsTable =  """
	                                           CREATE TABLE item_action (
	                                               id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
	                                               list_id BIGINT NOT NULL,
	                                               item_name VARCHAR(256) NOT NULL,
	                                               action CHAR(1) NOT NULL,
	                                               unit_price BIGINT NOT NULL,
	                                               amount INTEGER NOT NULL,
	                                               created_at BIGINT NOT NULL,
	                                               CONSTRAINT fk_item_action_list FOREIGN KEY (list_id) REFERENCES list(id) ON UPDATE NO ACTION ON DELETE CASCADE
	                                           );
	                                           """;
	    
	    return createItemActionsTable;
    }
}