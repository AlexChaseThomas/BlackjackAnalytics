#!/usr/bin/env python3
"""
generate_synthetic_data.py
BlackjackAnalytics — Phase 4 Synthetic Data Generator

Populates blackjack.db with realistic synthetic player sessions.
Simulates real blackjack game logic — all outcomes derived from actual
card probabilities using the same weighted probability tree traversal
model implemented in the C# game engine.

Six behavioral archetypes are distributed across 250 players with
individual variation added on top. Ages range from 21-68 to enable
age-based decision pattern analysis.

Run from the /analysis directory:
    python generate_synthetic_data.py

No external dependencies — uses only Python standard library.
"""

import sqlite3
import random
import os
import sys
from datetime import datetime, timedelta
from functools import lru_cache

# ── CONFIGURATION ──────────────────────────────────────────────────────────────

NUM_PLAYERS    = 250
SESSIONS_RANGE = (4, 7)
HANDS_RANGE    = (8, 16)

# Path to blackjack.db — adjust if your build output directory differs
DB_PATH = os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    '..', 'bin', 'Debug', 'net8.0', 'blackjack.db'
)

# Date range — synthetic sessions spread over 6 months leading to May 2026
DATE_START = datetime(2025, 11, 1)
DATE_END   = datetime(2026, 5, 22)

OS_VERSIONS = [
    "Microsoft Windows NT 10.0.22631.0",
    "Microsoft Windows NT 10.0.19045.0",
    "Microsoft Windows NT 10.0.22000.0",
    "Microsoft Windows NT 11.0.22631.0",
]

# ── NAMES ──────────────────────────────────────────────────────────────────────

FIRST_NAMES = [
    "Marcus","Tyler","James","Derek","Noah","Brandon","Oliver","Aiden","Ryan","Jason",
    "Kevin","Daniel","Chris","Michael","David","Andrew","Matthew","Joshua","Nathan","Eric",
    "Alex","Jordan","Dylan","Connor","Logan","Ethan","Lucas","Mason","Owen","Liam",
    "Carter","Hunter","Caleb","Austin","Cameron","Kyle","Sean","Aaron","Patrick","Zach",
    "Trevor","Garrett","Spencer","Brady","Cole","Blake","Jared","Cody","Travis","Seth",
    "Diane","Sandra","Kayla","Priya","Evelyn","Carmen","Ruth","Sarah","Jennifer","Ashley",
    "Melissa","Amanda","Stephanie","Jessica","Rebecca","Megan","Lauren","Allison","Hannah","Rachel",
    "Michelle","Nicole","Christina","Patricia","Barbara","Linda","Susan","Karen","Elizabeth","Lisa",
    "Amy","Angela","Heather","Brittany","Samantha","Amber","Danielle","Courtney","Tiffany","Vanessa",
    "Natalie","Maria","Laura","Kelly","Shannon","Whitney","Erica","Kimberly","Tara","Brianna",
]

LAST_NAMES = [
    "Webb","Kowalski","Nguyen","Okafor","Pritchard","Russo","Holt","Sharma","Castillo","Park",
    "Mills","Delgado","Chen","Walsh","Thompson","Williams","Johnson","Davis","Martinez","Anderson",
    "Taylor","Thomas","Moore","Jackson","White","Harris","Martin","Garcia","Brown","Smith",
    "Jones","Robinson","Clark","Rodriguez","Lewis","Lee","Walker","Hall","Allen","Young",
    "Hernandez","King","Wright","Lopez","Scott","Green","Adams","Baker","Nelson","Hill",
    "Ramirez","Campbell","Mitchell","Roberts","Carter","Phillips","Evans","Turner","Torres","Parker",
    "Collins","Edwards","Stewart","Flores","Morris","Murphy","Rivera","Cook","Rogers","Morgan",
    "Peterson","Cooper","Reed","Bailey","Bell","Gomez","Kelly","Howard","Ward","Cox",
    "Diaz","Richardson","Wood","Watson","Brooks","Bennett","Gray","Reyes","Hughes","Price",
    "Butler","Sanders","Foster","Powell","Jenkins","Perry","Russell","Sullivan","Patel","Kim",
    "Yamamoto","Okonkwo","Mensah","Tremblay","Beaumont","Leblanc","Kowalczyk","Fitzgerald","Callahan","Nakamura",
]

# ── BEHAVIORAL ARCHETYPES ──────────────────────────────────────────────────────
# Six player types with distinct behavioral fingerprints.
# Individual players receive values sampled from each range with added noise.
#
# Key dimensions:
#   strategy_on_prob  — probability strategy mode is ON for a given session
#   compliance_range  — range for how often the player follows recommendations
#   speed_range       — HandDurationSeconds distribution (base time before draws)
#   bet_range         — token bet range
#   double_down_prob  — how often the player doubles down when eligible
#   age_range         — age distribution for this archetype
#   weight            — relative frequency in the generated population

ARCHETYPES = {
    "disciplined": {
        "strategy_on_prob": 0.90,
        "compliance_range": (0.75, 0.95),
        "speed_range":      (8, 20),
        "bet_range":        (10, 35),
        "double_down_prob": 0.15,
        "age_range":        (28, 55),
        "weight":           0.18,
    },
    "impulsive": {
        "strategy_on_prob": 0.50,
        "compliance_range": (0.10, 0.35),
        "speed_range":      (1, 5),
        "bet_range":        (35, 100),
        "double_down_prob": 0.35,
        "age_range":        (21, 32),
        "weight":           0.15,
    },
    "deliberate": {
        "strategy_on_prob": 0.93,
        "compliance_range": (0.80, 0.97),
        "speed_range":      (20, 55),
        "bet_range":        (5, 25),
        "double_down_prob": 0.10,
        "age_range":        (40, 68),
        "weight":           0.17,
    },
    "casual": {
        "strategy_on_prob": 0.65,
        "compliance_range": (0.40, 0.70),
        "speed_range":      (6, 18),
        "bet_range":        (15, 55),
        "double_down_prob": 0.20,
        "age_range":        (25, 50),
        "weight":           0.20,
    },
    "risk_taker": {
        "strategy_on_prob": 0.45,
        "compliance_range": (0.15, 0.40),
        "speed_range":      (3, 12),
        "bet_range":        (50, 100),
        "double_down_prob": 0.40,
        "age_range":        (21, 40),
        "weight":           0.15,
    },
    "novice": {
        "strategy_on_prob": 0.70,
        "compliance_range": (0.20, 0.60),
        "speed_range":      (15, 45),
        "bet_range":        (5, 100),
        "double_down_prob": 0.08,
        "age_range":        (21, 65),
        "weight":           0.15,
    },
}

# ── CARD SYSTEM ────────────────────────────────────────────────────────────────

CARD_NAMES  = ["Ace","King","Queen","Jack","10","9","8","7","6","5","4","3","2"]
SUITS       = ["Hearts","Diamonds","Clubs","Spades"]
CARD_VALUES = {
    "Ace":11,"King":10,"Queen":10,"Jack":10,
    "10":10,"9":9,"8":8,"7":7,"6":6,"5":5,"4":4,"3":3,"2":2,
}

# Card weights for probability calculation — mirrors C# model
# 10/J/Q/K all have value 10 and weight 4; all others weight 1
_CW = {2:1, 3:1, 4:1, 5:1, 6:1, 7:1, 8:1, 9:1, 10:4, 11:1}

def build_deck():
    deck = [(n, s) for n in CARD_NAMES for s in SUITS]
    random.shuffle(deck)
    return deck

def deal_card(deck):
    if len(deck) < 10:
        deck.extend(build_deck())
    return deck.pop(0)

def hand_total(hand):
    """Calculate hand total with soft Ace adjustment."""
    total, aces = 0, 0
    for name, _ in hand:
        total += CARD_VALUES[name]
        if name == "Ace":
            aces += 1
    while total > 21 and aces > 0:
        total -= 10
        aces  -= 1
    return total

def hand_is_soft(hand):
    """Returns True if the hand contains an Ace currently counting as 11."""
    total, aces = 0, 0
    for name, _ in hand:
        total += CARD_VALUES[name]
        if name == "Ace":
            aces += 1
    while total > 21 and aces > 0:
        total -= 10
        aces  -= 1
    return aces > 0

# ── PROBABILITY ENGINE ─────────────────────────────────────────────────────────
# Mirrors the C# SimulateDealerDraw() weighted probability tree traversal.
# lru_cache ensures each unique game state is computed only once —
# transforming exponential recursion into linear memoized lookup.

@lru_cache(maxsize=None)
def _dealer_win_prob(player_total, dealer_total, dealer_aces):
    """
    P(dealer beats player_total from this dealer state).
    Recursive, memoized — mirrors C# SimulateDealerDraw() logic.
    """
    if dealer_total >= 17:
        return 1.0 if (dealer_total <= 21 and dealer_total > player_total) else 0.0
    prob = 0.0
    for cv, cw in _CW.items():
        nt = dealer_total + cv
        na = dealer_aces + (1 if cv == 11 else 0)
        while nt > 21 and na > 0:
            nt -= 10
            na -= 1
        prob += (cw / 13) * _dealer_win_prob(player_total, nt, na)
    return prob

def build_win_prob_table():
    """
    Pre-computes dealer win probability for all (playerTotal, dealerVisibleValue) pairs.
    Outer loop enumerates all possible dealer hole cards weighted by deck frequency —
    same approach as C# CalculateDealerWinProbability().
    Called once at startup; all in-simulation lookups are O(1).
    """
    table = {}
    for pt in range(4, 22):
        for dv in range(2, 12):
            prob = 0.0
            for hv, hw in _CW.items():
                da = (1 if dv == 11 else 0) + (1 if hv == 11 else 0)
                dt = dv + hv
                while dt > 21 and da > 0:
                    dt -= 10
                    da -= 1
                prob += (hw / 13) * _dealer_win_prob(pt, dt, da)
            table[(pt, dv)] = round(prob * 100, 1)
    return table

# ── STRATEGY LOGIC ─────────────────────────────────────────────────────────────

def get_recommendation(player_total, dv):
    """Basic strategy recommendation — mirrors C# GetStrategyRecommendation()."""
    if player_total >= 17: return "STAND"
    if player_total <= 11: return "HIT"
    if dv <= 6:
        if player_total >= 13: return "STAND"
        if player_total == 12 and dv >= 4: return "STAND"
        return "HIT"
    return "HIT"

def bust_chance_pct(total):
    """Bust probability as a percentage — mirrors C# CalculateBustChanceDouble()."""
    safe = 21 - total
    bust = sum(w for v, w in _CW.items()
               if (1 if v == 11 and v > safe else v) > safe)
    return bust / 13 * 100

def get_risk_level(bc):
    if bc <= 30: return "LOW"
    if bc <= 55: return "MODERATE"
    if bc <= 75: return "HIGH"
    return "VERY HIGH"

def determine_winner(pt, dt):
    """Mirrors C# DetermineWinner()."""
    if pt == 21 and dt == 21: return "Tie"
    if pt > 21:               return "Loss"
    if pt == 21:              return "Win"
    if dt == 21:              return "Loss"
    if dt > 21:               return "Win"
    if pt > dt:               return "Win"
    if dt > pt:               return "Loss"
    return "Tie"

# ── UTILITIES ──────────────────────────────────────────────────────────────────

def rand_dt(start, end):
    delta = int((end - start).total_seconds())
    if delta <= 0:
        return start
    return start + timedelta(seconds=random.randint(0, delta))

# ── HAND SIMULATION ────────────────────────────────────────────────────────────

def simulate_hand(player_id, username, player_age, game_num, session_id,
                  strategy_on, compliance, speed_range, bet_range,
                  dd_prob, token_balance, hand_time, os_ver, win_prob_table):
    """
    Simulates one complete blackjack hand.
    Returns a dict of all 29 GameSessions columns plus updated token_balance.
    Returns None if player cannot afford the minimum bet.
    """
    if token_balance < 5:
        return None

    max_bet = min(100, token_balance)
    _bet_lo = max(5, min(bet_range[0], max_bet))
    _bet_hi = max(_bet_lo, min(max_bet, bet_range[1]))
    bet = random.randint(_bet_lo, _bet_hi)
    tokens_before = token_balance

    # ── DEAL OPENING HANDS ─────────────────────────────────────────────────────
    deck           = build_deck()
    player_hand    = [deal_card(deck), deal_card(deck)]
    dealer_visible = deal_card(deck)
    dealer_hole    = deal_card(deck)
    dealer_hand    = [dealer_visible, dealer_hole]

    dv_name  = dealer_visible[0]
    dv_value = CARD_VALUES[dv_name]

    player_total         = hand_total(player_hand)
    dealer_total         = hand_total(dealer_hand)
    opening_player_total = player_total
    opening_dealer_total = dealer_total
    soft_opening         = hand_is_soft(player_hand)

    # ── TRACKING VARIABLES ─────────────────────────────────────────────────────
    recommended_action      = "NONE"
    dealer_win_prob         = 0.0
    risk_lvl                = "NONE"
    recommendation_followed = False
    overrode_suggestion     = False
    doubled_down            = False
    number_of_draws         = 0
    base_time               = random.randint(speed_range[0], speed_range[1])

    # ── PLAYER TURN ────────────────────────────────────────────────────────────
    if player_total != 21 and dealer_total != 21:

        # Set initial recommendation
        if strategy_on and opening_player_total >= 12:
            recommended_action = get_recommendation(player_total, dv_value)
            dealer_win_prob    = win_prob_table.get((min(player_total, 21), min(dv_value, 11)), 0.0)
            risk_lvl           = get_risk_level(bust_chance_pct(player_total))

        # Double down — only on opening two cards, totals 9-11
        can_double = token_balance >= bet * 2
        if can_double and 9 <= player_total <= 11 and random.random() < dd_prob:
            doubled_down    = True
            bet            *= 2
            player_hand.append(deal_card(deck))
            player_total    = hand_total(player_hand)
            number_of_draws = 1
            base_time      += random.randint(1, 4)

        else:
            # Normal drawing loop
            while player_total < 21:
                # Update recommendation at current total
                if strategy_on and player_total >= 12:
                    rec = get_recommendation(player_total, dv_value)
                    recommended_action = rec
                    dealer_win_prob    = win_prob_table.get(
                        (min(player_total, 21), min(dv_value, 11)), 0.0)
                    risk_lvl = get_risk_level(bust_chance_pct(player_total))

                    follows = random.random() < compliance
                    action  = rec if follows else ("HIT" if rec == "STAND" else "STAND")
                else:
                    # Strategy off — simple heuristic (hit below 17)
                    action = "HIT" if player_total < 17 else "STAND"

                if action == "STAND":
                    break

                player_hand.append(deal_card(deck))
                player_total    = hand_total(player_hand)
                number_of_draws += 1
                base_time       += random.randint(2, 5)

                if player_total >= 21:
                    break

        # Determine recommendation_followed and overrode_suggestion
        # Only meaningful when strategy was on and a tip was shown (total >= 12)
        if strategy_on and recommended_action != "NONE" and opening_player_total >= 12:
            if recommended_action == "STAND":
                recommendation_followed = (number_of_draws == 0)
            else:  # HIT
                recommendation_followed = (number_of_draws > 0)
            if not recommendation_followed:
                overrode_suggestion = True

    # ── DEALER TURN ────────────────────────────────────────────────────────────
    dealer_aces = sum(1 for n, _ in dealer_hand if n == "Ace")
    while dealer_total < 17:
        card = deal_card(deck)
        dealer_hand.append(card)
        if card[0] == "Ace":
            dealer_aces += 1
        dealer_total += CARD_VALUES[card[0]]
        while dealer_total > 21 and dealer_aces > 0:
            dealer_total -= 10
            dealer_aces  -= 1

    # ── RESULT AND TOKEN UPDATE ────────────────────────────────────────────────
    result = determine_winner(player_total, dealer_total)
    if result == "Win":
        token_balance += bet
    elif result == "Loss":
        token_balance -= bet

    hand_duration = max(1, base_time + random.randint(-1, 3))

    return {
        "session_id":               session_id,
        "player_id":                player_id,
        "username":                 username,
        "player_age":               player_age,
        "login_time":               hand_time.strftime("%Y-%m-%d %H:%M:%S"),
        "game_number":              game_num,
        "player_total":             player_total,
        "dealer_total":             dealer_total,
        "result":                   result,
        "player_busted":            1 if player_total > 21 else 0,
        "dealer_busted":            1 if dealer_total > 21 else 0,
        "number_of_draws":          number_of_draws,
        "bet_amount":               bet,
        "tokens_before":            tokens_before,
        "tokens_after":             token_balance,
        "strategy_mode":            "On" if strategy_on else "Off",
        "overrode_suggestion":      1 if overrode_suggestion else 0,
        "doubled_down":             1 if doubled_down else 0,
        "dealer_visible_card":      dv_name,
        "dealer_visible_value":     dv_value,
        "opening_player_total":     opening_player_total,
        "opening_dealer_total":     opening_dealer_total,
        "player_hand_was_soft":     1 if soft_opening else 0,
        "hand_duration_seconds":    hand_duration,
        "os_version":               os_ver,
        "recommended_action":       recommended_action,
        "recommendation_followed":  1 if recommendation_followed else 0,
        "risk_level":               risk_lvl,
        "dealer_win_probability":   dealer_win_prob,
        "_token_balance":           token_balance,   # internal — not written to DB
        "_result":                  result,          # internal — used for streak tracking
    }

# ── MAIN GENERATOR ─────────────────────────────────────────────────────────────

def generate(num_players=NUM_PLAYERS):
    print("=" * 54)
    print("  BlackjackAnalytics — Synthetic Data Generator")
    print("=" * 54)
    print(f"  Target     : {num_players} players")
    print(f"  Database   : {os.path.abspath(DB_PATH)}")
    print()

    if not os.path.exists(DB_PATH):
        print(f"ERROR: Database not found at:\n  {os.path.abspath(DB_PATH)}")
        print("\nAdjust DB_PATH at the top of the script to point to your blackjack.db.")
        sys.exit(1)

    # ── PRE-COMPUTE PROBABILITY TABLE ──────────────────────────────────────────
    print("Building probability lookup table...", end=" ", flush=True)
    win_prob_table = build_win_prob_table()
    print(f"done. ({len(win_prob_table)} entries cached)\n")

    conn = sqlite3.connect(DB_PATH)
    cur  = conn.cursor()

    # Load existing usernames to avoid collisions with real player data
    cur.execute("SELECT Username FROM Players")
    existing_usernames = {row[0] for row in cur.fetchall()}
    print(f"  Existing players in database : {len(existing_usernames)}")

    # ── GENERATE PLAYER PROFILES ───────────────────────────────────────────────
    archetype_names   = list(ARCHETYPES.keys())
    archetype_weights = [ARCHETYPES[a]["weight"] for a in archetype_names]

    players    = []
    used_names = set(existing_usernames)

    while len(players) < num_players:
        first    = random.choice(FIRST_NAMES)
        last     = random.choice(LAST_NAMES)
        username = (first + last).lower()

        if username in used_names:
            continue

        used_names.add(username)
        archetype  = random.choices(archetype_names, weights=archetype_weights, k=1)[0]
        arch       = ARCHETYPES[archetype]

        players.append({
            "username":   username,
            "age":        random.randint(*arch["age_range"]),
            "archetype":  archetype,
            "compliance": random.uniform(*arch["compliance_range"]),
            "os_version": random.choice(OS_VERSIONS),
        })

    print(f"  Synthetic players to generate: {len(players)}\n")
    print(f"  {'Player':>6}  {'Sessions':>8}  {'Hands':>10}")
    print(f"  {'------':>6}  {'--------':>8}  {'----------':>10}")

    total_hands    = 0
    total_sessions = 0

    for i, player in enumerate(players):
        arch       = ARCHETYPES[player["archetype"]]
        compliance = player["compliance"]
        os_ver     = player["os_version"]

        first_seen = rand_dt(DATE_START, DATE_END - timedelta(days=14))
        last_seen  = rand_dt(first_seen + timedelta(days=1), DATE_END)

        # ── INSERT PLAYER ROW ──────────────────────────────────────────────────
        cur.execute("""
            INSERT INTO Players
                (Username, PlayerAge, FirstSeen, LastSeen, TokenBalance,
                 TotalHandsAllTime, TotalWinsAllTime, FavoriteStrategyMode, LongestWinStreak)
            VALUES (?, ?, ?, ?, 100, 0, 0, ?, 0)
        """, (
            player["username"], player["age"],
            first_seen.strftime("%Y-%m-%d %H:%M:%S"),
            last_seen.strftime("%Y-%m-%d %H:%M:%S"),
            "On" if arch["strategy_on_prob"] >= 0.5 else "Off",
        ))
        player_id = cur.lastrowid

        token_balance         = 100
        total_hands_player    = 0
        total_wins_player     = 0
        longest_streak_player = 0
        current_streak        = 0

        num_sessions  = random.randint(*SESSIONS_RANGE)
        session_dates = sorted([
            rand_dt(first_seen, last_seen) for _ in range(num_sessions)
        ])

        for session_date in session_dates:
            # Award daily bonus if player is low on tokens
            if token_balance < 5:
                token_balance += 50

            strategy_on   = random.random() < arch["strategy_on_prob"]
            session_id    = random.randint(100000, 999999)
            start_balance = token_balance
            session_time  = session_date
            session_hands = []

            for game_num in range(1, random.randint(*HANDS_RANGE) + 1):
                if token_balance < 5:
                    break

                hand = simulate_hand(
                    player_id, player["username"], player["age"],
                    game_num, session_id, strategy_on, compliance,
                    arch["speed_range"], arch["bet_range"], arch["double_down_prob"],
                    token_balance, session_time, os_ver, win_prob_table,
                )

                if hand is None:
                    break

                token_balance = hand["_token_balance"]
                session_time += timedelta(
                    seconds=hand["hand_duration_seconds"] + random.randint(8, 25)
                )

                # Streak tracking
                if hand["_result"] == "Win":
                    current_streak        += 1
                    total_wins_player     += 1
                    longest_streak_player  = max(longest_streak_player, current_streak)
                else:
                    current_streak = 0

                session_hands.append(hand)

            # ── INSERT SESSION ROW ─────────────────────────────────────────────
            cur.execute("""
                INSERT OR IGNORE INTO Sessions
                    (SessionID, PlayerID, Username, StartTime, EndTime,
                     TotalHands, StartBalance, EndBalance, NetProfit)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                session_id, player_id, player["username"],
                session_date.strftime("%Y-%m-%d %H:%M:%S"),
                session_time.strftime("%Y-%m-%d %H:%M:%S"),
                len(session_hands), start_balance,
                token_balance, token_balance - start_balance,
            ))

            # ── INSERT HAND ROWS ───────────────────────────────────────────────
            for h in session_hands:
                cur.execute("""
                    INSERT INTO GameSessions (
                        SessionID, PlayerID, Username, PlayerAge, LoginTime,
                        GameNumber, PlayerTotal, DealerTotal, Result,
                        PlayerBusted, DealerBusted, NumberOfDraws,
                        BetAmount, TokensBefore, TokensAfter,
                        StrategyMode, OverrodeSuggestion, DoubledDown,
                        DealerVisibleCard, DealerVisibleValue,
                        OpeningPlayerTotal, OpeningDealerTotal,
                        PlayerHandWasSoft, HandDurationSeconds, OSVersion,
                        RecommendedAction, RecommendationFollowed,
                        RiskLevel, DealerWinProbability
                    ) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)
                """, (
                    h["session_id"],    h["player_id"],      h["username"],
                    h["player_age"],    h["login_time"],      h["game_number"],
                    h["player_total"],  h["dealer_total"],    h["result"],
                    h["player_busted"], h["dealer_busted"],   h["number_of_draws"],
                    h["bet_amount"],    h["tokens_before"],   h["tokens_after"],
                    h["strategy_mode"], h["overrode_suggestion"], h["doubled_down"],
                    h["dealer_visible_card"],  h["dealer_visible_value"],
                    h["opening_player_total"], h["opening_dealer_total"],
                    h["player_hand_was_soft"], h["hand_duration_seconds"], h["os_version"],
                    h["recommended_action"],   h["recommendation_followed"],
                    h["risk_level"],           h["dealer_win_probability"],
                ))

            total_hands    += len(session_hands)
            total_sessions += 1
            total_hands_player += len(session_hands)

        # ── UPDATE PLAYER LIFETIME STATS ───────────────────────────────────────
        cur.execute("""
            UPDATE Players
            SET TokenBalance      = ?,
                TotalHandsAllTime = ?,
                TotalWinsAllTime  = ?,
                LongestWinStreak  = ?,
                LastSeen          = ?
            WHERE PlayerID = ?
        """, (
            token_balance, total_hands_player, total_wins_player,
            longest_streak_player,
            last_seen.strftime("%Y-%m-%d %H:%M:%S"),
            player_id,
        ))

        # Progress reporting and periodic commits
        if (i + 1) % 50 == 0 or (i + 1) == len(players):
            conn.commit()
            print(f"  {i+1:>6,}  {total_sessions:>8,}  {total_hands:>10,}")

    conn.commit()
    conn.close()

    print()
    print("=" * 54)
    print(f"  Complete")
    print(f"  Players  : {len(players):,}")
    print(f"  Sessions : {total_sessions:,}")
    print(f"  Hands    : {total_hands:,}")
    print(f"  Database : {os.path.abspath(DB_PATH)}")
    print("=" * 54)


if __name__ == "__main__":
    generate()
