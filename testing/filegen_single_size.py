import os
import random
import sys

FILE_EXTENSIONS = [".txt", ".jpg", ".bin", ".log", ".csv"]
FILE_COUNT = 10
FILE_SIZES_KB = [
    1000,
    5000,
    10000,
    50000,
    100000,
    500000,
    1000000
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

def main():
    if len(sys.argv) > 1:
        output_dir = sys.argv[1].strip()
    else:
        print(f"📁 Podaj ścieżkę do folderu testów:")
        output_dir = input().strip()

    if not os.path.exists(output_dir):
        print(f"Ten folder nie istnieje: {output_dir}")
        exit(1)

    output_dir = os.path.join(output_dir, 'single_size')
    print(f"📁 Tworzę folder dla zestawu testów...")
    ensure_dir(output_dir)

    for size in FILE_SIZES_KB:
        size_bytes = size * 1024
        print(f"📁 Tworzę folder dla testu z wielkością {size}KB...")
        size_output_dir = os.path.join(output_dir, f"test_single_size_{size}KB")
        ensure_dir(size_output_dir)

        for i in range(FILE_COUNT):
            ext = random.choice(FILE_EXTENSIONS)
            filename = f"file_{i:04d}_{size}KB{ext}"
            file_path = os.path.join(size_output_dir, filename)
            generate_file(file_path, size_bytes)

    print(f"✅ Gotowe!.")    

if __name__ == "__main__":
    main()