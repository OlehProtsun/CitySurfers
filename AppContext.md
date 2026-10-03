# AppContext.md

# Project Context for AI

> This document is the primary high-level product context for the application.
> Any AI agent, developer, designer, product manager, or code-generation assistant working on this project should read this file first.
>
> The goal is to make the complete product idea understandable without requiring additional explanation.

---

## 1. Project Overview

**Project name:** TBD  
**Working description:** Gamified running platform for runners in Kraków

The product is a mobile-first running application that transforms real-world running into a competitive game.

Instead of showing only standard fitness metrics such as distance, pace, time, and calories, the application adds:

- live competition;
- overtakes;
- city rankings;
- monthly seasons;
- rivals;
- 1v1 races;
- route competitions;
- a city activity map;
- personal progression;
- an AI running coach.

The central product idea is:

> Turn every run into a sequence of short, visible, achievable competitive goals.

The application should make the user feel that continuing for another 500 meters, 1 kilometer, or several minutes has an immediate purpose.

Example:

Instead of:

> "You still need to run 1 km."

The user sees:

> "1.1 km more and you will overtake Runner_42 and move from #38 to #37 in today's ranking."

The application therefore combines fitness tracking with game mechanics, social competition, and personal progression.

---

# 2. Problem

Many runners stop a workout earlier than planned because there is no immediate consequence for stopping.

Example:

A person planned to run 5 km.

At 4 km they feel tired and think:

> "Nothing really happens if I stop now."

Traditional running apps usually show statistics, but statistics alone are often not enough to create immediate motivation.

This project solves that problem by introducing real-time goals connected to other runners.

Instead of abstract progress, the user sees concrete competitive targets:

- one more kilometer to pass another runner;
- five more minutes to gain a ranking position;
- a rival only 13 points ahead;
- another runner catching them during a live race;
- a nearby route title that can be challenged.

The user always has a possible next objective.

---

# 3. Product Vision

The product should feel like a real-world multiplayer running game.

Real physical activity becomes the input for a game layer.

A completed run can affect:

- daily ranking;
- monthly season ranking;
- personal rating;
- rivalry status;
- route ranking;
- achievements;
- personal progress;
- live race results.

The product must still remain useful to runners who are not highly competitive.

Competition is one motivation layer.

Personal development is another.

The application should therefore maintain two separate concepts:

1. **Competitive progress**
2. **Personal fitness progress**

They should influence the user experience differently.

---

# 4. Target Audience

The application is designed for all levels of runners:

- beginners;
- casual runners;
- regular runners;
- experienced runners.

The system must not require professional-level running performance.

A new user can begin at a base level.

The system gradually learns their real performance based on actual runs.

The application should avoid making beginners feel permanently disadvantaged.

A user's personal improvement should matter even if they are not near the top of the city leaderboard.

---

# 5. Core User Loop

The main recurring product loop is:

1. User starts a run.
2. The app tracks or receives running activity.
3. The app compares the user's performance with relevant competitors.
4. The app creates short competitive goals.
5. The user sees who they can overtake or who is catching them.
6. The user continues running.
7. The app awards points and updates rankings.
8. The run contributes to:
   - daily competition;
   - monthly season;
   - route progress;
   - personal progress.
9. The app summarizes what happened.
10. The user receives a new reason to run again.

A successful experience should make the user think:

> "I can run a little more because I am close to passing someone."

and later:

> "I want to run again because I am close to the next position."

---

# 6. Running Data

A run can be recorded directly in the application or imported from a supported fitness service.

The system can analyze data such as:

- distance;
- duration;
- pace;
- speed;
- running history;
- previous personal results;
- consistency;
- current ranking;
- current rating;
- recent performance;
- opponent performance;
- route information;
- season activity.

Not every metric must directly produce points.

The scoring system should combine multiple factors rather than reward raw distance only.

---

# 7. Daily / Live Ranking

During a run, the user can see their current competitive position among relevant active runners.

Example UI state:

```text
#38 Today

6.4 km
5:12 / km

Runner_92 is ahead.

1.1 km to overtake.

At your current pace:
~5 minutes
```

After completing the target:

```text
OVERTAKE!

You passed Runner_92

#38 -> #37

+18 points
```

The most important motivational mechanic is the **overtake**.

The user should not only see:

- how far they have run;

but also:

- who is ahead;
- how far away that person is in the competition;
- what the user needs to do to pass them;
- how the ranking will change;
- how many points can potentially be earned.

---

# 8. Overtake System

An overtake is a game event in which the user's current performance becomes better than another relevant runner's result according to the active competition rules.

The opponent does not necessarily need to be physically in front of the user.

An overtake can be virtual.

Example:

```text
Marta is ahead

700 m
```

Later:

```text
420 m
```

Then:

```text
180 m

YOU ARE CATCHING MARTA
```

Finally:

```text
OVERTAKE!

You overtook Marta.
```

The experience should create the feeling of a real race even if both runners are in different parts of Kraków.

---

# 9. Virtual Live Competition

Users do not need to run on the same road.

Example:

- User A is running near Błonia.
- User B is running near the Vistula boulevards.
- Both are active at approximately the same time.
- The system compares their live activity.
- The application can present a virtual gap between them.

This is a core product principle:

> Physical location does not have to determine competitive proximity.

The platform can create a live competitive layer using activity data.

---

# 10. Game Points

Points should not be awarded only for kilometers.

The point system can consider:

- who the user overtook;
- opponent level;
- opponent rating;
- current ranking position;
- speed;
- pace;
- distance;
- personal historical performance;
- improvement compared with previous results;
- difficulty of the overtake;
- live race result;
- season activity.

Example principle:

Passing a stronger runner should generally be worth more than passing a significantly weaker runner.

The system should also consider the user's own historical level.

This prevents the platform from becoming a pure "who can run the most kilometers" competition.

---

# 11. Game Rating

**Game Rating** represents the user's competitive position or strength inside the platform.

It answers:

> "Where do I stand compared with other runners?"

Game Rating can be influenced by:

- overtakes;
- live races;
- ranking position;
- strength of opponents;
- current performance;
- activity during the season;
- competitive results.

Game Rating should be treated separately from simple monthly point totals.

Possible conceptual distinction:

- **Points** = progression currency / season score.
- **Game Rating** = competitive strength / relative position.

The exact rating algorithm is TBD.

---

# 12. Monthly Season

Each calendar month acts as a new game season.

Example:

```text
OCTOBER SEASON

#1   2814 pts
#2   2761 pts
#3   2690 pts
...
#74  You — 1438 pts
```

During a season users can:

- earn points;
- climb the leaderboard;
- lose positions;
- overtake other runners;
- defend their position;
- compete against nearby rivals;
- participate in races;
- compete for route titles.

A season should provide a recurring reset and a fresh reason to return.

Historical seasons should remain available for profile history and achievements.

---

# 13. Rivals

The system can identify runners close to the user in ranking or rating.

Example:

```text
YOUR RIVAL

You       1438 pts
Mateusz   1451 pts

Difference:
13 points
```

A rival should represent a realistic short-term target.

A good rival is not necessarily a friend.

The system may select a rival because they are:

- close in ranking;
- close in Game Rating;
- similar in activity level;
- frequently competing for the same positions.

The rival system should create motivation without requiring direct communication.

---

# 14. 1v1 Live Race

Users can create direct live running challenges.

Example challenge:

```text
8 KM RACE
```

Another user accepts.

Both users can run anywhere.

The system compares their progress in real time.

Example:

```text
YOU
4.82 km

ALEX
4.61 km

You are 210 m ahead.
```

Later:

```text
Alex increased pace.

Gap:
210 m -> 120 m

Alex is catching you.
```

Final result:

```text
YOU WON

8.00 km — 42:18

Alex:
8.00 km — 43:02
```

1v1 races can happen:

- between friends;
- between rivals;
- between matched random users;
- during special events.

Potential race parameters may include:

- target distance;
- start window;
- countdown;
- maximum allowed completion time;
- race status;
- live progress;
- result.

Exact matchmaking and race rules are TBD.

---

# 15. Kraków Running Activity Map

The application contains a map focused initially on Kraków.

The map visualizes where running activity is concentrated.

Privacy is important.

The application should **not expose precise private live coordinates of individual users by default**.

The public map should primarily display aggregated activity.

Example:

```text
Błonia
124 runners today

Bulwary Wiślane
89 runners today

Zakrzówek
42 runners today
```

Map filters can include:

- Live;
- Today;
- Month.

The map can show:

- popular running areas;
- popular routes;
- active districts;
- number of runs;
- number of active runners;
- average pace;
- route length;
- trending routes;
- activity by time of day.

The map should make Kraków feel like an active running game world.

---

# 16. Route System

The platform can define popular running routes.

A route may include:

- name;
- approximate path;
- distance;
- popularity;
- number of runs;
- average pace;
- active runners;
- monthly activity;
- ranking.

Example routes could include areas such as:

- Błonia;
- Bulwary Wiślane;
- Zakrzówek.

Routes can have their own competitive systems.

---

# 17. King of Route

Popular routes can have a special route leaderboard.

Example:

```text
BŁONIA LOOP

King of Route:
Marek

184 km this month

Your position:
#14
```

The **King of Route** is the leading user for a route according to a route-specific scoring formula.

The title should not be based only on one extremely long run.

Possible factors:

- frequency;
- performance;
- activity;
- consistency;
- distance;
- repeat participation;
- route-specific results.

A King of Route can receive:

- a special badge;
- crown icon;
- special profile styling;
- special marker on the route page;
- special marker on the map.

The exact route scoring algorithm is TBD.

---

# 18. Personal Progress

Personal Progress is intentionally separated from Game Rating.

It answers:

> "Am I becoming a better runner compared with my previous self?"

Example:

```text
YOUR MONTH

Average pace:
5:42 -> 5:28

Weekly distance:
17 km -> 23 km

Consistency:
+16%

Running performance:
Improving
```

Personal progress can analyze:

- average pace;
- weekly distance;
- monthly distance;
- run frequency;
- consistency;
- duration;
- speed;
- personal bests;
- trend over time.

This part of the app should remain rewarding even when the user is not winning competitive rankings.

---

# 19. AI Coach

The AI Coach analyzes the user's running history and explains progress in natural, simple language.

The AI Coach should not merely repeat statistics.

It should translate data into understandable observations.

Example:

```text
Your average pace improved this month from 5:42/km to 5:28/km.

You also increased your weekly distance without reducing your training consistency.

Your strongest improvement was in longer runs, where your pace stayed more stable than last month.
```

Possible AI Coach responsibilities:

- summarize recent progress;
- identify positive trends;
- identify declining consistency;
- explain new personal records;
- compare recent activity with the user's own history;
- explain why the user's performance score changed;
- suggest realistic training goals;
- summarize a completed run;
- prepare a weekly or monthly progress summary.

The AI Coach should use the user's own data as the main reference point.

It should avoid making medical diagnoses.

Health or injury-related situations should be handled conservatively and should not be presented as professional medical advice.

The AI Coach is intended to support motivation and understanding, not replace a professional coach or doctor.

---

# 20. Post-Run Summary

After a run, the user should receive a game-oriented summary rather than only fitness statistics.

Possible summary:

```text
RUN COMPLETE

7.4 km
38:21
5:11 / km

3 overtakes

#41 -> #36 Today

+72 season points

New personal best:
Fastest 5 km this month

Rival gap:
21 pts -> 6 pts
```

The post-run experience should answer:

- What did I achieve?
- Who did I pass?
- How did my ranking change?
- How many points did I earn?
- Did I improve personally?
- What is my next target?

---

# 21. Main Application Areas

The application can conceptually contain the following main areas.

## Home / Live Run

Primary running screen.

Shows:

- map;
- current run;
- distance;
- time;
- pace;
- current ranking;
- closest target;
- distance to overtake;
- live events.

## Ranking

Shows:

- today ranking;
- monthly season ranking;
- Game Rating;
- rivals;
- position changes.

## Map

Shows:

- aggregated runner activity;
- popular running zones;
- routes;
- trending routes;
- route competitions.

## Races

Shows:

- active 1v1 races;
- race invitations;
- matchmaking;
- previous races;
- special events.

## Progress

Shows:

- personal statistics;
- historical trends;
- achievements;
- personal bests;
- AI Coach insights.

## Profile

Shows:

- username;
- level / rating;
- current season;
- badges;
- route titles;
- race history;
- personal achievements.

---

# 22. Important UX Principle: Always Show the Next Goal

The interface should constantly look for an actionable next target.

Examples:

```text
320 m to overtake Kamil
```

```text
8 points to pass your rival
```

```text
1.4 km to enter the Top 50 today
```

```text
2 more runs this week to maintain your consistency streak
```

```text
You are 14 seconds from your monthly 5K best
```

The product should avoid overwhelming users with too many goals simultaneously.

During a live run, one primary objective should usually be visually dominant.

---

# 23. Motivation Design

The motivation system is built from several layers.

### Immediate motivation

Seconds to minutes.

Examples:

- catch another runner;
- defend position;
- finish another kilometer;
- close a live-race gap.

### Session motivation

One running session.

Examples:

- complete 3 overtakes;
- enter Top 30 today;
- beat a personal run target.

### Short-term motivation

Days.

Examples:

- pass a rival;
- improve weekly consistency;
- reclaim a ranking position.

### Seasonal motivation

One month.

Examples:

- finish Top 100;
- reach a new rating;
- become King of Route.

### Long-term motivation

Multiple months.

Examples:

- personal improvement;
- season history;
- badges;
- performance trends;
- competitive identity.

---

# 24. Privacy Principles

Location privacy is a core requirement.

The application should not publicly expose an individual runner's exact private GPS location without explicit design and permission.

Public map activity should preferably be aggregated.

Examples of safe public representations:

- heatmaps;
- approximate active zones;
- route-level activity;
- number of runners in an area;
- delayed or generalized location data.

Private data can still be used internally for:

- recording a run;
- validating route activity;
- calculating progress;
- determining route participation;
- live competition logic.

The exact privacy model is TBD and must be treated as a first-class product requirement.

---

# 25. Fairness and Anti-Cheat

Because competitive ranking is based on physical activity, the system should be designed with cheating prevention in mind.

Potential abuse cases include:

- recording activity while driving;
- GPS manipulation;
- unrealistic speed;
- duplicate imports;
- manually modified fitness data;
- impossible acceleration patterns;
- repeated fake routes.

The application should eventually include activity validation.

Possible signals:

- plausible running speed;
- GPS consistency;
- duplicate detection;
- source trust level;
- suspicious pace patterns;
- device / provider metadata.

Exact anti-cheat rules are TBD.

Do not design the ranking system assuming all submitted activity is automatically trustworthy.

---

# 26. Ranking Design Principles

When implementing ranking logic, preserve these product principles:

1. Raw kilometers alone must not determine the entire game.
2. Stronger opponents should generally be more valuable to beat.
3. Personal improvement should have value.
4. Consistency should matter.
5. A single extreme activity should not automatically dominate long-term systems.
6. Beginners should have meaningful goals.
7. Highly active runners should still have challenging competition.
8. Ranking changes should be understandable to the user.
9. The app should be able to explain why points were earned.
10. Competitive systems should resist easy exploitation.

---

# 27. Conceptual Data Entities

These are conceptual product entities, not a mandatory database schema.

## User

Possible fields:

- id;
- username;
- avatar;
- city;
- level;
- gameRating;
- seasonPoints;
- currentRank;
- badges;
- privacy settings.

## Run

Possible fields:

- id;
- userId;
- startTime;
- endTime;
- duration;
- distance;
- averagePace;
- averageSpeed;
- GPS track;
- source;
- validation status.

## Season

Possible fields:

- id;
- month;
- year;
- startDate;
- endDate;
- leaderboard.

## Overtake

Possible fields:

- id;
- userId;
- opponentId;
- runId;
- timestamp;
- pointsAwarded;
- rankingBefore;
- rankingAfter.

## Race

Possible fields:

- id;
- type;
- creatorId;
- opponentId;
- targetDistance;
- status;
- startTime;
- finishTime;
- winnerId.

## Route

Possible fields:

- id;
- name;
- area;
- distance;
- path;
- popularity;
- activityStatistics.

## RouteRanking

Possible fields:

- routeId;
- userId;
- score;
- position;
- month.

## Rival

Possible fields:

- userId;
- rivalUserId;
- scoreDifference;
- rankDifference;
- activeFrom.

---

# 28. Event-Oriented Product Model

Many important actions can be represented as game events.

Examples:

- RUN_STARTED
- RUN_COMPLETED
- OVERTAKE_STARTED
- OVERTAKE_COMPLETED
- RANK_CHANGED
- POINTS_AWARDED
- RIVAL_CHANGED
- RACE_CREATED
- RACE_ACCEPTED
- RACE_STARTED
- RACE_GAP_CHANGED
- RACE_COMPLETED
- PERSONAL_BEST
- ROUTE_COMPLETED
- ROUTE_RANK_CHANGED
- KING_OF_ROUTE_CHANGED
- SEASON_STARTED
- SEASON_ENDED

This event model can be useful for:

- notifications;
- live UI;
- backend logic;
- analytics;
- activity feeds;
- achievements.

---

# 29. Notifications

Notifications should be meaningful and game-oriented.

Possible examples:

```text
Mateusz passed you.
You are now #42.
```

```text
Your rival is only 8 points ahead.
```

```text
You lost your King of Route title on Błonia Loop.
```

```text
3.2 km today could move you into the Top 50.
```

```text
Alex challenged you to an 8 km race.
```

Notifications should avoid excessive spam.

The system should prioritize events that create a realistic reason to open the app or run.

---

# 30. Social Layer

The product can support social competition without becoming a traditional social network.

Possible social features:

- friends;
- rivals;
- race invitations;
- profile viewing;
- badges;
- achievements;
- activity summaries;
- route titles.

A full public post/feed system is not required by the current core concept.

---

# 31. City-First Strategy

The initial product is designed specifically around **Kraków**.

This is important because local density makes the game more meaningful.

Kraków should not be treated as a generic placeholder city.

The city itself is part of the product experience.

The platform can use:

- local routes;
- local activity zones;
- city rankings;
- route kings;
- local events;
- Kraków-specific competition.

Expansion to other cities can happen later.

---

# 32. What Makes the Product Different

The application is not intended to be only:

- a GPS tracker;
- a leaderboard;
- a Strava clone;
- a running statistics dashboard;
- an AI fitness coach.

Its main differentiation is the combination of:

- real running;
- live virtual overtakes;
- local city competition;
- dynamic micro-goals;
- monthly seasons;
- route ownership;
- direct races;
- personal improvement.

The strongest emotional moment should be:

> "I am catching someone."

followed by:

> "I passed them."

---

# 33. Example Complete User Session

A user opens the app.

The home screen shows:

```text
Today: #41

Closest target:
Runner_92

0.9 km to overtake
```

The user starts a run.

After 2 km:

```text
You are catching Runner_92.

Gap:
540 m
```

Later:

```text
190 m
```

Then:

```text
OVERTAKE!

#41 -> #40

+16 points
```

The app immediately finds another realistic target:

```text
Next:
Marta

0.7 km ahead
```

The user decides to continue running.

At the end:

```text
6.8 km

2 overtakes

#41 -> #38

+41 season points

Average pace:
5:26 / km

Your rival is now only 9 points ahead.
```

The AI Coach adds:

```text
You ran slightly faster than your recent average and maintained your pace better during the final 2 km.
```

The user now has another reason to return:

```text
9 points to pass your rival.
```

This is the intended product loop.

---

# 34. MVP Priority

The likely MVP should focus on proving the main motivational mechanic.

Suggested priority:

## Core MVP

- user account;
- start / record a run;
- distance / pace / duration tracking;
- daily ranking;
- season points;
- basic monthly leaderboard;
- live or simulated overtake targets;
- post-run summary;
- personal progress;
- Kraków activity map with privacy-safe aggregated data.

## Secondary

- rivals;
- route rankings;
- King of Route;
- imported fitness activities;
- richer AI Coach.

## Later / Advanced

- fully synchronized 1v1 live races;
- random matchmaking;
- special events;
- sophisticated anti-cheat;
- advanced route ownership systems;
- cross-city expansion.

This priority is a product interpretation of the concept and can be changed.

---

# 35. Open Product Questions

The following parts are intentionally not fully defined yet.

AI agents should not silently invent permanent rules for them.

They should either:

- make a clearly stated temporary assumption;
- propose options;
- or ask for a product decision when necessary.

Open questions include:

- final project name;
- exact point formula;
- exact Game Rating algorithm;
- exact definition of a live overtake;
- which runners are eligible to be compared;
- how often rankings update;
- whether imported activities can participate in live systems;
- season reset behavior;
- rating decay;
- rival selection rules;
- King of Route formula;
- route detection;
- exact privacy model;
- live location sharing rules;
- anti-cheat thresholds;
- fitness service integrations;
- notification frequency;
- monetization;
- achievements / levels;
- city expansion model.

---

# 36. Rules for AI Agents Working on This Project

When generating product ideas, code, architecture, UI, database schemas, copy, or business logic for this project:

1. Preserve the core idea of **running as a competitive game**.
2. Treat **overtakes** as one of the most important mechanics.
3. Keep **Game Rating** and **Personal Progress** conceptually separate.
4. Do not reduce the scoring model to raw distance only.
5. Preserve privacy around exact runner locations.
6. Treat Kraków as the first real city, not merely sample data.
7. Prefer short, actionable goals during a run.
8. Keep the system understandable to beginners.
9. Do not invent final scoring formulas unless explicitly requested.
10. Clearly mark assumptions when product rules are still TBD.
11. Keep live UI focused and avoid excessive information while the user is running.
12. Design systems so that cheating can be detected or limited later.
13. Prefer explainable ranking and point changes.
14. Personal improvement should remain rewarding even if the user loses competitive positions.
15. The application should feel like a game layered on top of real physical activity.

---

# 37. One-Sentence Product Definition

> A Kraków-first gamified running platform that turns real runs into live overtakes, city rankings, monthly seasons, route competitions, direct races, and measurable personal progress.

---

# 38. Short AI Summary

If an AI needs the shortest possible understanding of the application:

> Users run in the real world. The app tracks or imports their activity and converts it into a multiplayer game. During a run, the user sees other runners as virtual competitive targets and receives live goals such as "700 m to overtake Marta." Overtakes, races, performance, opponent strength, consistency, and personal improvement contribute to points and rankings. Each month is a season. Kraków has an aggregated running activity map, popular routes, route leaderboards, and a King of Route system. Separate from competitive ranking, the app tracks personal progress and uses an AI Coach to explain improvement. Exact user locations should remain private, and ranking systems should be designed to resist cheating.

---

## Status

This document describes the current product concept.

Technical architecture, backend stack, frontend framework, database choice, final scoring formulas, and production infrastructure are not yet defined in this context.
