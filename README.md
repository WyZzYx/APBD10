Thats how ConnectionString should look like
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<YOUR_SERVER>;Database=<YOUR_DATABASE>;ACCEPT_EULA=Y;SA_PASSWORD=<YOUR_PASSWORD>"
  },
}

I Chose different projects for this solution, because it will be easier to extend the solution in the future making it easier for debugging and structurisation. 
We always want to separate our solution into different projects, when we expect the project to extend.
