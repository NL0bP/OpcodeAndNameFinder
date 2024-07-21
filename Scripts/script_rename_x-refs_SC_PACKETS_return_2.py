import idaapi
import idautils
import idc

def read_rename_list(file_path):
    rename_dict = {}
    with open(file_path, 'r') as file:
        for line in file:
            parts = line.strip().split()
            if len(parts) == 2:
                old_name, new_name = parts
                rename_dict[old_name] = new_name
    return rename_dict

def rename_xrefs(target_name, rename_dict):
    target_addr = idc.get_name_ea_simple(target_name)
    if target_addr == idc.BADADDR:
        print(f"Target name '{target_name}' not found.")
        return

    for xref in idautils.XrefsTo(target_addr):
        xref_addr = xref.frm
        xref_name = idc.get_name(xref_addr)
        if xref_name in rename_dict:
            new_name = rename_dict[xref_name]
            if idc.set_name(xref_addr, new_name, idc.SN_CHECK):
                print(f"Renamed {xref_name} to {new_name}")
            else:
                print(f"Failed to rename {xref_name} to {new_name}")

def main():
    file_path = "SCOffsets.cs_IDA.txt"  # Укажите путь к вашему текстовому файлу
    rename_dict = read_rename_list(file_path)
    target_name = "SC_PACKETS_return_2"
    rename_xrefs(target_name, rename_dict)

if __name__ == "__main__":
    main()