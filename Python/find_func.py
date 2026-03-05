import json

with open('RAG_SWP.ipynb', encoding='utf-8') as f:
    nb = json.load(f)

# Find all cells and their order
for cell_idx, cell in enumerate(nb['cells']):
    cell_id = cell.get('id', f'index_{cell_idx}')
    exec_count = cell.get('execution_count')
    source_start = ''.join(cell.get('source', []))[:60].replace('\n', ' ')
    has_build_func = 'def _build_and_save_index' in ''.join(cell.get('source', []))
    
    if has_build_func or exec_count in [13, 14, 15, 16, 17]:
        print(f"Cell {cell_idx}: ID={cell_id}, exec_count={exec_count}")
        print(f"  Source: {source_start}")
        if has_build_func:
            print(f"  *** THIS CONTAINS _build_and_save_index DEFINITION ***")
        print()


