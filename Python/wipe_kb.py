import os
import shutil
from pathlib import Path

kb_dir = Path(r"d:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\kb")
if kb_dir.exists():
    for item in kb_dir.iterdir():
        try:
            if item.is_file():
                item.unlink()
                print(f"Deleted file: {item.name}")
            elif item.is_dir():
                shutil.rmtree(item)
                print(f"Deleted dir: {item.name}")
        except Exception as e:
            print(f"Failed to delete {item.name}: {e}")
else:
    print("KB directory does not exist.")
