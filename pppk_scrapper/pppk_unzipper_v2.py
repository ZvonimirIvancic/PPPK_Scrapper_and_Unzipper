import os 
import gzip  
import shutil

def gz_extract(directory):
    extension = '.gz'
    os.chdir(directory)
    for item in os.listdir(directory):
        if item.endswith(extension):
            gz_name = os.path.abspath(item)
            file_name = (os.path.basename(gz_name)).rsplit('.', 1)[0]
            with gzip.open(gz_name, "rb") as f_in, open(file_name, "wb") as f_out:
                shutil.copyfileobj(f_in, f_out)
            #os.remove(gz_name)

desktop_dir = os.path.join(os.path.expanduser("~"), "Desktop", "XenaDownloads")
gz_extract(desktop_dir)