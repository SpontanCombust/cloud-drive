import os
import random
import sys

FILE_EXTENSIONS = [".txt", ".jpg", ".bin", ".log", ".csv"]
FILE_SIZE_SUM_BYTES = 2 ** 30 # ~1GB
FILE_COUNTS = [
    10,
    25,
    50,
    75,
    100,
    250,
    500,
    750,
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

def main():
    if len(sys.argv) > 1:
        output_dir = sys.argv[1].strip()
    else:
        print(f"📁 Podaj ścieżkę do folderu testów:")
        output_dir = input().strip()

    if not os.path.exists(output_dir):
        print(f"Ten folder nie istnieje: {output_dir}")
        exit(1)

    output_dir = os.path.join(output_dir, 'whole_size')
    print(f"📁 Tworzę folder dla zestawu testów...")
    ensure_dir(output_dir)

    for count in FILE_COUNTS:
        print(f"📁 Tworzę folder dla testu z ilością {count}...")
        count_output_dir = os.path.join(output_dir, f"test_whole_size_{count}")
        ensure_dir(count_output_dir)

        size_bytes = FILE_SIZE_SUM_BYTES // count
        size_kb = size_bytes // 1024

        for i in range(count):
            ext = random.choice(FILE_EXTENSIONS)
            filename = f"file_{i:04d}_{size_kb}KB{ext}"
            file_path = os.path.join(count_output_dir, filename)
            generate_file(file_path, size_bytes)

    print(f"✅ Gotowe!.")    

if __name__ == "__main__":
    main()