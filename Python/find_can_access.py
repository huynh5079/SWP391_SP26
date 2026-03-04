import json

with open('RAG_SWP.ipynb', encoding='utf-8') as f:
    nb = json.load(f)

for cell_idx, cell in enumerate(nb['cells']):
    source = ''.join(cell.get('source', []))
    if 'def can_access' in source:
        print(f"Cell {cell_idx}:")
        # Find the function definition
        lines = source.split('\n')
        start_idx = next(i for i, line in enumerate(lines) if 'def can_access' in line)
        # Print ~20 lines starting from the definition
        for i in range(start_idx, min(start_idx + 20, len(lines))):
            print(lines[i])
        print()
