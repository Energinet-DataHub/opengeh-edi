#!/bin/bash

# Check if the path to the config file and the name of the Key Vault are provided
if [ -z "$1" ] || [ -z "$2" ]; then
  echo "Usage: $0 <path-to-config-file> <keyvault-name>"
  exit 1
fi

CONFIG_FILE_PATH=$1
VAULT_NAME=$2

az account show >/dev/null 2>&1
if [ $? != 0 ]; then
  echo "Please sign in with Azure CLI using 'az login' command."
  az login
  if [ $? != 0 ]; then
    echo "Failed to sign in with Azure CLI."
    exit 1
  fi
fi

# Extract the base name of the config file and create a new filename
BASE_NAME=$(basename "$CONFIG_FILE_PATH")
OUTPUT_FILE="${BASE_NAME%.*}-generated.${BASE_NAME##*.}"

# Check if the output file already exists and remove it if it does
if [ -f "$OUTPUT_FILE" ]; then
  rm "$OUTPUT_FILE"
fi

# Rest of your script using $CONFIG_FILE_PATH
echo "Config file path: $CONFIG_FILE_PATH"
echo "Vault name: $VAULT_NAME"

# Read through the input file and find all templates
while IFS= read -r line; do
  # Check if the line contains a template
  if [[ $line =~ \{\{([^}]+)\}\} ]]; then
    template="${BASH_REMATCH[1]}"
    
    # Find the corresponding secret in keyvault
    secret=$(az keyvault secret show --vault-name "$VAULT_NAME" --name "$template" --query "value" -o tsv 2>/dev/null)
    
    # Check if the secret was found
    if [ -z "$secret" ]; then
      echo "Secret '$template' not found in Key Vault '$VAULT_NAME'."
    else
      # Replace the template with the secret value in the line
      line="${line//\{\{$template\}\}/$secret}"
    fi
  fi
  
  # Process the modified line
  echo "$line" >> "$OUTPUT_FILE"
done < "$CONFIG_FILE_PATH"

echo "Output written to $OUTPUT_FILE"