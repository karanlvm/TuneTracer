# TuneTracer's Algorithm


## 1. WAV Loading & Preprocessing
- **Raw PCM extraction**: We parse the RIFF/WAVE header ourselves (no external libraries) to load 16 bit PCM samples.
- **Spectrogram (STFT)**: Audio is framed into 2048-sample windows (≈46 ms) with 50% overlap, a Hann window is applied, and a radix-2 FFT is run to produce a high-resolution magnitude spectrogram \(S[t,f]\).

## 2. Peak Detection (“Constellation”)
- **Local maxima**: For each time frame and frequency bin, we detect peaks that exceed a dynamic threshold (mean + 1.5 σ) and are the highest in a 7×7 neighborhood.
- **Node list**: Each peak \(p_i = (t_i, f_i, A_i)\) becomes a node in our fingerprint graph.

## 3. Graph-Based Descriptors
- **Neighbor graph**: For each peak \(p_i\), we connect it to up to K = 5 future peaks \(p_j\) within ±Δf (e.g. ±50 bins) and Δt (up to 1 s).
- **Amplitude weighting**: Each edge is weighted by \(A_i \times A_j\), so louder events carry more significance.
- **Local descriptor**: We record \((\Delta t, \Delta f, w)\) for each of the K strongest edges, forming a 3×K vector around each anchor.

## 4. Quantization & Hashing
- **Bins**: \(\Delta t\) quantized into 16 time-bins, \(\Delta f\) into 32 freq-bins, and \(w\) into 8 amplitude-bins.
- **FNV-1a hashing**: The 3K bytes are concatenated and hashed into a 64-bit fingerprint code, compactly encoding subgraph structure.

## 5. Persistent Indexing
- All `(code, songId, timestamp)` tuples are bulk‐inserted into SQLite with an index on `code`.
- This on-disk database scales to millions of fingerprints and requires no external key-value service.

## 6. Query & Matching
1. **Fingerprint snippet**: The same STFT → peaks → graph → hashes pipeline runs on the query.
2. **Lookup**: Each code is looked up in SQLite, returning `(songId, dbTimestamp)` entries.
3. **Voting**: We tally votes for each `(songId, Δt = queryTime – dbTime)` pair.
4. **Best match**: The song and time-offset with the highest vote count wins.

## Why Tune Tracer Beats Classic Shazam
- **Richer descriptors**: Captures a small subgraph of K edges per anchor vs. a single peak pair.
- **Amplitude-aware**: Weighs edges by peak magnitudes for robustness to noise.
- **Finer quantization**: More granular time/frequency bins for higher discrimination.
