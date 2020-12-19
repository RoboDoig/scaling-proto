using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aws.GameLift.Server;
using Aws.GameLift;

using DarkRift;
using DarkRift.Server;

namespace ScalingPlugins
{
    public class GameLift
    {
        // Set port that game service will listen on for incoming player connections
        public int listeningPort = -1;

        // Game preparation state
        private bool gameSessionInfoReceived = false;
        private float waitingForPlayerTime = 0f;

        // Game state
        private bool gameStarted = false;
        private string gameSessionId;
        public string GetGamSessionID() { return gameSessionId; }

        public GameLift()
        {
            listeningPort = 7777;

            //InitSDK establishes a local connection with the Amazon GameLift agent to enable 
            //further communication.
            var initSDKOutcome = GameLiftServerAPI.InitSDK();
            if (initSDKOutcome.Success)
            {
                ProcessParameters processParameters = new ProcessParameters(
                    (gameSession) => {
                    //Respond to new game session activation request. GameLift sends activation request 
                    //to the game server along with a game session object containing game properties 
                    //and other settings.

                    // Activate the session
                    GameLiftServerAPI.ActivateGameSession();

                    //Start waiting for players
                    this.gameSessionInfoReceived = true;
                        this.gameSessionId = gameSession.GameSessionId;
                    },
                    () => {
                    //OnProcessTerminate callback. GameLift invokes this callback before shutting down 
                    //an instance hosting this game server. It gives this game server a chance to save
                    //its state, communicate with services, etc., before being shut down. 
                    //In this case, we simply tell GameLift we are indeed going to shut down.
                    GameLiftServerAPI.ProcessEnding();
                    },
                    () => {
                    //This is the HealthCheck callback.
                    //GameLift invokes this callback every 60 seconds or so.
                    //Here, a game server might want to check the health of dependencies and such.
                    //Simply return true if healthy, false otherwise.
                    //The game server has 60 seconds to respond with its health status. 
                    //GameLift will default to 'false' if the game server doesn't respond in time.
                    //In this case, we're always healthy!
                    return true;
                    },
                    //Here, the game server tells GameLift what port it is listening on for incoming player 
                    //connections. We will use the port received from command line arguments
                    listeningPort,
                    new LogParameters(new List<string>()
                    {
                    //Let GameLift know where our logs are stored. We are expecting the command line args to specify the server with the port in log file
                    "/local/game/logs/myserver"+listeningPort+".log"
                    }));

                //Calling ProcessReady tells GameLift this game server is ready to receive incoming game sessions
                var processReadyOutcome = GameLiftServerAPI.ProcessReady(processParameters);
                if (processReadyOutcome.Success)
                {
                    Console.WriteLine("ProcessReady success.");
                }
                else
                {
                    Console.WriteLine("ProcessReady failure : " + processReadyOutcome.Error.ToString());
                }
            }
            else
            {
                Console.WriteLine("InitSDK failure : " + initSDKOutcome.Error.ToString());
            }
        }
    }
}
