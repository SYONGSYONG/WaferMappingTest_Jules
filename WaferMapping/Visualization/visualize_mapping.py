import pandas as pd
import matplotlib.pyplot as plt
import sys
import os

def visualize_mapping(csv_path, output_path=None):
    if not os.path.exists(csv_path):
        print(f"Error: CSV file not found at {csv_path}")
        return

    try:
        df = pd.read_csv(csv_path)
    except Exception as e:
        print(f"Error reading CSV: {e}")
        return

    plt.figure(figsize=(12, 10))

    # Filter by State
    # State=1: Existing Chip
    # State=0: Missing/Empty

    existing = df[df['State'] == 1]
    missing = df[df['State'] == 0]

    # Plot Missing Chips (State=0) as faint gray dots to show map shape
    plt.scatter(missing['TrueX'], missing['TrueY'], c='lightgray', marker='.', alpha=0.3, label='Empty/Missing', s=10)

    # Plot Existing Chips (State=1) - True Position
    plt.scatter(existing['TrueX'], existing['TrueY'], c='blue', alpha=0.5, label='Existing Chip (True)', s=25)

    # Plot Predicted Position for Existing Chips only
    plt.scatter(existing['PredictedX'], existing['PredictedY'], c='red', marker='x', alpha=0.6, label='Predicted (Affine)', s=25)

    # Plot Anchors (Green circles, larger)
    anchors = df[df['IsAnchor'] == True]
    plt.scatter(anchors['TrueX'], anchors['TrueY'], c='green', s=150, facecolors='none', edgecolors='green', linewidth=2, label='Anchors')

    # Draw error lines only for existing chips
    for _, row in existing.iterrows():
        plt.plot([row['TrueX'], row['PredictedX']], [row['TrueY'], row['PredictedY']], color='gray', alpha=0.2, linewidth=0.5)

    plt.title('Wafer Mapping with Defects: True vs Predicted')
    plt.xlabel('Stage X (mm)')
    plt.ylabel('Stage Y (mm)')
    plt.legend()
    plt.grid(True)
    plt.axis('equal')

    if output_path:
        plt.savefig(output_path, dpi=150)
        print(f"Visualization saved to {output_path}")

    # Show the plot window and block until closed
    print("Displaying plot... Close the window to exit.")
    plt.show()

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python visualize_mapping.py <input_csv> [output_image]")
        sys.exit(1)

    csv_file = sys.argv[1]
    out_file = sys.argv[2] if len(sys.argv) > 2 else None

    visualize_mapping(csv_file, out_file)
