import pandas as pd
import matplotlib.pyplot as plt
import sys
import os

def visualize_mapping(csv_path, output_path):
    if not os.path.exists(csv_path):
        print(f"Error: CSV file not found at {csv_path}")
        return

    try:
        df = pd.read_csv(csv_path)
    except Exception as e:
        print(f"Error reading CSV: {e}")
        return

    plt.figure(figsize=(12, 10))

    # Plot True Grid (Blue dots)
    plt.scatter(df['TrueX'], df['TrueY'], c='blue', alpha=0.3, label='True Position', s=20)

    # Plot Predicted Grid (Red crosses)
    plt.scatter(df['PredictedX'], df['PredictedY'], c='red', marker='x', alpha=0.5, label='Predicted Position', s=20)

    # Plot Anchors (Green circles, larger)
    anchors = df[df['IsAnchor'] == True]
    plt.scatter(anchors['TrueX'], anchors['TrueY'], c='green', s=100, facecolors='none', edgecolors='green', linewidth=2, label='Anchors')

    # Draw error lines
    for _, row in df.iterrows():
        plt.plot([row['TrueX'], row['PredictedX']], [row['TrueY'], row['PredictedY']], color='gray', alpha=0.2, linewidth=0.5)

    plt.title('Wafer Mapping Visualization: True vs Predicted')
    plt.xlabel('Stage X (mm)')
    plt.ylabel('Stage Y (mm)')
    plt.legend()
    plt.grid(True)
    plt.axis('equal')

    plt.savefig(output_path, dpi=150)
    print(f"Visualization saved to {output_path}")

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: python visualize_mapping.py <input_csv> <output_image>")
        sys.exit(1)

    visualize_mapping(sys.argv[1], sys.argv[2])
