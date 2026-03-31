# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Run (interactive)
dotnet run

# Run with arguments: [member_name] [column_length_mm] [eccentricity_ratio]
dotnet run -- "300A PIPE" 4470 0.25

# Release build
dotnet publish -c Release
```

There are no tests in this project.

## Architecture

This is a .NET 8 console application that calculates the maximum safe working load for steel columns under buckling conditions, implementing AISC buckling formulas.

### Layers

- **`Program.cs`** — CLI entry point; parses args, orchestrates the two services, prints formatted output
- **`Models/`** — `MemberProfile` (cross-section dimensions) and `BucklingInput` (calculation parameters)
- **`Servies/`** (typo for Services) — two services:
  - `MemberDatabaseService` — loads `PropertyRefer.txt` (tab-delimited) into an in-memory dictionary; replaces Excel VLOOKUP
  - `BucklingCalculatorService` — implements the AISC buckling algorithm

### Calculation Flow

1. Load member cross-section properties from `PropertyRefer.txt`
2. Compute elastic critical stress via Euler's formula: `Fe = (π² × E × I) / (L² × A)`
3. Determine AISC slenderness limit: `λ_limit = 4.71 × √(E / Fy)`
4. Compute inelastic critical stress `Fcr` (AISC formula, two branches based on slenderness)
5. For **concentric loading** (`e = 0`): working load = `Fcr × A / safety_factor`
6. For **eccentric loading** (`e ≠ 0`): iterative Secant formula from 100% down to 0.1% (0.5% steps) to find max `P` where `σ_max ≤ Fy`

### Default Parameters

| Parameter | Value |
|---|---|
| Elastic Modulus (E) | 210,000 MPa |
| Yield Stress (Fy) | 240 MPa |
| Safety Factor | 3.0 |

### Member Types in `PropertyRefer.txt`

Four categories: H-profiles, Pipe sections (PIPE), I.A sections (asymmetric, uses centroid offset), and I-profiles. The I.A sections have special handling for eccentricity: `refDistance = ReferenceDim − CentroidY` (all others use `ReferenceDim / 2`).

### Output Format

Output is a single decimal place value (`:F1`) in metric tons, formatted for machine parsing by an external Python server.
