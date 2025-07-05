import os
import sys
import random
import numpy as np
from math import log

FILE_EXTENSIONS = [".txt", ".jpg", ".bin", ".log", ".csv"]
FILE_SIZE_SUM_BYTES = 2 ** 30  # ~1GB
FILE_COUNTS = [
    5, 
    10, 
    20, 
    50, 
    100, 
    200, 
    500, 
    1000
]

def ensure_dir(path):
    os.makedirs(path, exist_ok=True)

def generate_file(file_path, size_bytes):
    chunk_size = 1024 * 1024 # 1MB
    with open(file_path, "wb") as f:
        remaining = size_bytes
        while remaining > 0:
            # Determine current chunk size (don't exceed remaining bytes)
            current_chunk = min(chunk_size, remaining)
            # Generate and write chunk
            f.write(os.urandom(current_chunk))
            remaining -= current_chunk

def generate_sizes(total_bytes, count):
    """Generate file sizes with log-normal distribution"""
    # Parameters for log-normal distribution (adjust these to shape your distribution)
    mu = log(100 * 1024)  # Median around 100KB
    sigma = 10.0  # Controls the spread
    
    # Generate log-normally distributed sizes
    sizes = []
    
    # Generate until we have enough sizes that sum to approximately the target
    while len(sizes) < count * 2:  # Generate extra to allow for selection
        size = int(np.random.lognormal(mu, sigma))
        if 10 <= size <= 10 * 1024 * 1024 * 10:  # Limit between 100B and 100MB
            sizes.append(size)
    
    # Normalize to exactly match our total size requirement
    selected = []
    total_generated = sum(sizes)
    
    for size in sizes:
        adjusted_size = max(int(size * total_bytes / total_generated), 10)
        selected.append(adjusted_size)
        if len(selected) == count:
            break
    
    # Final adjustment to ensure exact total
    selected[-1] += total_bytes - sum(selected)
    
    return selected

def main():
    if len(sys.argv) > 1:
        output_dir = sys.argv[1].strip()
    else:
        print(f"📁 Podaj ścieżkę do folderu testów:")
        output_dir = input().strip()

    if not os.path.exists(output_dir):
        print(f"Ten folder nie istnieje: {output_dir}")
        exit(1)

    output_dir = os.path.join(output_dir, 'mixed')
    print(f"📁 Tworzę folder dla zestawu testów...")
    ensure_dir(output_dir)

    for count in FILE_COUNTS:
        print(f"📁 Tworzę folder dla testu z ilością {count}...")
        count_output_dir = os.path.join(output_dir, f"test_mixed_{count}")
        ensure_dir(count_output_dir)

        # Generate normally distributed sizes
        sizes = generate_sizes(FILE_SIZE_SUM_BYTES, count)
        random.shuffle(sizes)  # Shuffle for more natural distribution

        # print(sum(sizes) / (10 ** 9))

        for i, size_bytes in enumerate(sizes):
            size_kb = size_bytes // 1024
            ext = random.choice(FILE_EXTENSIONS)
            filename = f"file_{i:04d}_{size_kb}KB{ext}"
            file_path = os.path.join(count_output_dir, filename)
            generate_file(file_path, size_bytes)

    print("✅ Gotowe!")

if __name__ == "__main__":
    main()