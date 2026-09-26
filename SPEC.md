## Lindencourt FX System — Final Specification

### 1. Entry (4 mandatory conditions)

#### 1.1 BUY
1. EMA(7) crossed above EMA(21)
2. Candle crossed and closed above EMA(84)
3. T3 CCI(5) >= 0 at candle open
4. RSI Histo(13) has >=1 closed bar >= +10

#### 1.2 SELL — mirrored

### 2. Exit

#### 2.1 Stop Loss
- Fast pairs: 35 pips
- Slow pairs: 25 pips
- Maximum: 40 pips

#### 2.2 System Exit
- Candle closed against EMA(7) **AND** T3 CCI(5) crossed 0

#### 2.3 Partial exits
- Exit 1: 50% at Fib Pivot (R1/R2/...)
- Exit 2: 20% at next Fib Pivot
- Exit 3: 10%
- Exit 4: 10%
- Exit 5: 10% (System Exit)

#### 2.4 Break-even
- After Exit 1 → move SL to break-even
- Only if price is >= 30 pips from entry

### 3. Money Management
- Risk <= 2% per trade (1% recommended)
- Max 2 pairs at a time
- Leverage <= 100:1

### 4. Fibonacci Pivots
- P = (H+L+C)/3
- R1 = P + (H−L)×0.382, R2 = P + (H−L)×0.618, R3 = P + (H−L)×1.0
- R4 = P + (H−L)×1.618, R5 = P + (H−L)×2.618
- S1–S5 — mirrored
- MR1 = (P+R1)/2, MR2 = (R1+R2)/2, ...
- Day starts at **17:00 NY** (`Destination__HrsNewTZfromGMT = -5` or `-4`)

### 5. Indicator Settings (Appendix 1)

#### TzPivots
- GMT MT4: `Local=0`, `Dest=2`
- GMT+1 MT4: `Local=1`, `Dest=2`
- (3rd parameter needs clarification from screenshots)

#### RSI Histo
- Fixed Min = −50, Fixed Max = +50
- Multiplier = **1.5** (per MT4 template screenshot)

#### T3 CCI
- CCI_Period = 5
- T3_Period = 5
- b = 0.618
- Colors: 0 = Black, 1,2 = None

#### i-Sessions
- **GMT winter:** Asia 00–09, Eur 08–17, US 13–22
- **GMT summer:** Asia 00–09, Eur 07–16, US 12–21
- **GMT+1 winter:** Asia 01–10, Eur 09–18, US 14–23
- **GMT+1 summer:** Asia 02–11, Eur 09–18, US 14–23

### 6. Trading Hours
- **No entry before 06:45 GMT**
- Recommended: 06:00–16:00 GMT
- Best windows: 07:00 GMT (Asia+Europe), 13:00 GMT (Europe+US)
- Most active days: Wednesday, Thursday
- Monday — quietest
- News: wait 15 min

### 7. Target Performance
- **+35 net pips/day**
- ~50% of trades yield 50+ pips (May 2009)
- ~33% of trades yield 50+ pips (Aug–Sep 2009)

### 8. Not Coded (discretionary)
- Candlestick patterns
- Chart patterns
- "Gut feeling"
- News (a simple filter could be added)