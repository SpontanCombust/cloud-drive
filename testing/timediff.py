import pandas as pd
from datetime import datetime

def calculate_time_range(csv_file, start_line, end_line):
    """
    Calculate the time range between the earliest start time and latest end time
    in a specified line range of the CSV file.
    
    Args:
        csv_file (str): Path to the CSV file
        start_line (int): First line to include (1-indexed)
        end_line (int): Last line to include (1-indexed)
    
    Returns:
        dict: Contains min_start, max_end, and time_difference in milliseconds
    """
    # Read the specified range of lines (adjusting for 0-indexing and header)
    df = pd.read_csv(csv_file, delimiter=';', header=None, 
                    skiprows=start_line-1, nrows=end_line-start_line+1)
    
    # Assign column names based on your format
    df.columns = ['start_time', 'end_time', 'test_name', 'unknown', 'file_path']
    
    # Convert datetime strings to datetime objects
    df['start_time'] = pd.to_datetime(df['start_time'], format='%d.%m.%Y %H:%M:%S.%f')
    df['end_time'] = pd.to_datetime(df['end_time'], format='%d.%m.%Y %H:%M:%S.%f')
    
    # Find min start time and max end time
    min_start = df['start_time'].min()
    max_end = df['end_time'].max()
    
    # Calculate time difference in milliseconds
    time_diff_ms = (max_end - min_start).total_seconds() * 1000
    
    return {
        'min_start': min_start.strftime('%d.%m.%Y %H:%M:%S.%f')[:-3],  # trim microseconds to 3 digits
        'max_end': max_end.strftime('%d.%m.%Y %H:%M:%S.%f')[:-3],
        'time_difference_ms': time_diff_ms,
        'time_difference_str': f"{time_diff_ms:.2f} ms"
    }

if __name__ == "__main__":
    # Get user input
    csv_file = "C:\\Users\\X\\AppData\\Roaming\\CloudDrive\\benchmarks.csv"
    start_line = int(input("Enter start line number (1-indexed): "))
    end_line = int(input("Enter end line number (1-indexed): "))
    
    # Calculate and display results
    try:
        result = calculate_time_range(csv_file, start_line, end_line)
        print("\nResults:")
        print(f"Earliest start time: {result['min_start']}")
        print(f"Latest end time:    {result['max_end']}")
        print(f"Total time range:   {result['time_difference_str']}")
    except Exception as e:
        print(f"Error: {e}")