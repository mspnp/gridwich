#! /bin/bash
echo "Confirming current subscription..."

currentSubscription=$(az account show --query id)
if [[ $currentSubscription != "\"5eec61d7-ed64-4048-a3bd-133bfef1d7a2\"" ]]; then
  echo "Invalid subscription"
  exit 1
fi

if [ -n "$1" ]; then 
  echo "Looking for resource groups with 'environment' tag '$1' ..."
else
  echo "Must specify environment tag value as parameter"
  exit 1
fi

envResources=$(az group list --query "[?tags.environment=='$1'].name" --output json)
echo "Found these resource groups: "
echo $(echo $envResources | jq -C --tab .)

echo 
while : ; do
  echo "Press 'y' to continue and delete these resource groups"
  read -rsn 1 k
  if [[ $k != y ]] ; then
    exit 0
  else
    break
  fi
done

envResourceSpaces=$(echo $envResources | tr -d ',' | tr -d '[' | tr -d ']' | tr -d '"')
resources=($envResourceSpaces)

for rg in ${resources[@]}
do
  echo deleting $rg ...
  az group delete --name $rg --no-wait --yes
done
