const { TableClient, AzureNamedKeyCredential, odata } = require("@azure/data-tables");

const accountName = process.env.STORAGE_ACCOUNT_NAME;
const accountKey = process.env.STORAGE_ACCOUNT_KEY;
const tableName = "highscores";

const credential = new AzureNamedKeyCredential(accountName, accountKey);
const client = new TableClient(
    `https://${accountName}.table.core.windows.net`,
    tableName,
    credential
);

module.exports = async function (context, req) {
    if (req.method === "GET") {
        try {
            let highscores = [];
            // Query with filtering and ordering
            const entities = client.listEntities({
                queryOptions: {
                    filter: "",  // Get all scores
                    select: ["playerName", "score", "timestamp"],
                    top: 10     // Limit to top 10 records
                }
            });

            for await (const entity of entities) {
                highscores.push({
                    playerName: entity.playerName,
                    score: parseInt(entity.score),
                    timestamp: entity.timestamp || new Date(entity.Timestamp).toISOString()
                });
            }

            // Sort by score in descending order
            highscores.sort((a, b) => b.score - a.score);

            context.res = {
                status: 200,
                headers: {
                    'Content-Type': 'application/json'
                },
                body: {
                    scores: highscores.slice(0, 10),
                    totalCount: highscores.length,
                    timestamp: new Date().toISOString()
                }
            };
        } catch (error) {
            context.log.error('Error retrieving highscores:', error);
            context.res = {
                status: 500,
                body: {
                    error: "Error retrieving highscores",
                    details: error.message
                }
            };
        }
    } else if (req.method === "POST") {
        try {
            const { playerName, score } = req.body;

            // Validate input
            if (!playerName || typeof score !== 'number' || score < 0) {
                context.res = {
                    status: 400,
                    body: {
                        error: "Invalid input",
                        details: "Player name and valid score are required"
                    }
                };
                return;
            }

            const timestamp = new Date().toISOString();
            const entity = {
                partitionKey: "highscores",
                rowKey: `${timestamp}_${Math.random().toString(36).substr(2, 9)}`,
                playerName,
                score: score.toString(),
                timestamp
            };

            await client.createEntity(entity);
            
            // Get updated top scores
            const topScores = [];
            const queryResults = client.listEntities({
                queryOptions: { top: 10 }
            });

            for await (const result of queryResults) {
                topScores.push({
                    playerName: result.playerName,
                    score: parseInt(result.score)
                });
            }

            topScores.sort((a, b) => b.score - a.score);
            
            context.res = {
                status: 200,
                body: {
                    message: "Score saved successfully",
                    newScore: {
                        playerName,
                        score,
                        timestamp
                    },
                    topScores: topScores.slice(0, 10)
                }
            };
        } catch (error) {
            context.log.error('Error saving score:', error);
            context.res = {
                status: 500,
                body: {
                    error: "Error saving score",
                    details: error.message
                }
            };
        }
    }
};
