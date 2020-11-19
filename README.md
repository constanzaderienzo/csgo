# CS2D 8-bit


### Controls

|  Action       |  Button       | Alternative |
|:-------------:|:-------------:|:-----------:|
| Move Left     | `A`  |    ---      |
| Move Right    | `D` |    ---      |
| Move Up       | `W`    |    ---      |
| Move Down     | `X`  |    ---      |
| Walk      | `Shift + Direction`  |    ---      |
| Crouch      | `Ctrl + Direction`  |    ---      |
| Move sight    | `Mouse`       |    ---      |
| Change weapon | `Scroll wheel` |`1`, `2`, `3`, `4`|

### Game
The game has two modes: a 'team' mode and a deathmatch mode. 
To begin one must run a server selecting either mode. Once the server is up and running a client can be run, specifying the address and a username (which must be unique, if its taken the server will reply specifying that). If the server address is incorrect or something wrong happened, an error will also appear indicating that it was unable to connect.

Each player starts with the following weapons which vary in **damage** and **range**:
|  Weapon       |  Damage       | Range |
|:-------------:|:-------------:|:-----------:|
| Pistol     | 20  |    25      |
| AK-47    | 50 |    40      |
| Sniper Rifle       | 100    |    200      |
| MAC-10     | 30  |    25      |
| Shotgun      | 80  |    10      |

As mentioned above the player has three different movement speeds:
* Running
* Walking
* Crouching

Each of these in addition of changing the speed of the movement, change the sound the player makes while walking making it harder or easier to spot for an enemy player. 

Whenever a kill has occurred a message will appear at the top right of the screen indicating who killed who.

Finally, to exit one can press Escape and a side menu will open allowing the player to Disconnect from the server.

#### Team mode
In the this mode the player spawns in either the **terrorist** or **counter-terrorist** team. Each team has a specific spawn site. 

|  Terrorists       |  Counter-terrorists       |
|:-------------:|:-------------:|
|![](https://i.imgur.com/k6vY5WA.png)|![](https://i.imgur.com/6f7COZJ.png)

It works with rounds, so if a round has already started and a player wants to join **he will have to wait** until the current round ends. 
A round is won when either team runs out of players. Furthermore, the game is won when 2 rounds are won by either team (current number set for showcasing and testing the game rather than actual gameplay).
When the game is won a scoreboard table is displayed showing the players and their kills. 

#### Deathmatch mode
A "free for all" where players have no teams and must try to score the most points. 


### Code

The main code files are: 
* Server files: 
    * ServerCS for team mode
    * ServerDeathmatch for deathmatch mode
* Code files:
    * ClientCS for team mode
    * ClientDeathmatch for deathmatch mode

* LobbyCS which handles the logic for the menu
* WorldInfo
* Snapshot which contains packet number and world info
* ClientInfo which holds some additional useful information
* ClientEntity which holds the gameobjects
