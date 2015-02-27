using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FiledRecipes.Domain
{
    /// <summary>
    /// Holder for recipes.
    /// </summary>
    public class RecipeRepository : IRecipeRepository
    {
        /// <summary>
        /// Represents the recipe section.
        /// </summary>
        private const string SectionRecipe = "[Recept]";

        /// <summary>
        /// Represents the ingredients section.
        /// </summary>
        private const string SectionIngredients = "[Ingredienser]";

        /// <summary>
        /// Represents the instructions section.
        /// </summary>
        private const string SectionInstructions = "[Instruktioner]";

        /// <summary>
        /// Occurs after changes to the underlying collection of recipes.
        /// </summary>
        public event EventHandler RecipesChangedEvent;

        /// <summary>
        /// Specifies how the next line read from the file will be interpreted.
        /// </summary>
        private enum RecipeReadStatus { Indefinite, New, Ingredient, Instruction };

        /// <summary>
        /// Collection of recipes.
        /// </summary>
        private List<IRecipe> _recipes;

        /// <summary>
        /// The fully qualified path and name of the file with recipes.
        /// </summary>
        private string _path;

        /// <summary>
        /// Indicates whether the collection of recipes has been modified since it was last saved.
        /// </summary>
        public bool IsModified { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the RecipeRepository class.
        /// </summary>
        /// <param name="path">The path and name of the file with recipes.</param>
        public RecipeRepository(string path)
        {
            // Throws an exception if the path is invalid.
            _path = Path.GetFullPath(path);

            _recipes = new List<IRecipe>();
        }

        /// <summary>
        /// Returns a collection of recipes.
        /// </summary>
        /// <returns>A IEnumerable&lt;Recipe&gt; containing all the recipes.</returns>
        public virtual IEnumerable<IRecipe> GetAll()
        {
            // Deep copy the objects to avoid privacy leaks.
            return _recipes.Select(r => (IRecipe)r.Clone());
        }

        /// <summary>
        /// Returns a recipe.
        /// </summary>
        /// <param name="index">The zero-based index of the recipe to get.</param>
        /// <returns>The recipe at the specified index.</returns>
        public virtual IRecipe GetAt(int index)
        {
            // Deep copy the object to avoid privacy leak.
            return (IRecipe)_recipes[index].Clone();
        }

        /// <summary>
        /// Deletes a recipe.
        /// </summary>
        /// <param name="recipe">The recipe to delete. The value can be null.</param>
        public virtual void Delete(IRecipe recipe)
        {
            // If it's a copy of a recipe...
            if (!_recipes.Contains(recipe))
            {
                // ...try to find the original!
                recipe = _recipes.Find(r => r.Equals(recipe));
            }
            _recipes.Remove(recipe);
            IsModified = true;
            OnRecipesChanged(EventArgs.Empty);
        }

        /// <summary>
        /// Deletes a recipe.
        /// </summary>
        /// <param name="index">The zero-based index of the recipe to delete.</param>
        public virtual void Delete(int index)
        {
            Delete(_recipes[index]);
        }

        /// <summary>
        /// Raises the RecipesChanged event.
        /// </summary>
        /// <param name="e">The EventArgs that contains the event data.</param>
        protected virtual void OnRecipesChanged(EventArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of 
            // a race condition if the last subscriber unsubscribes 
            // immediately after the null check and before the event is raised.
            EventHandler handler = RecipesChangedEvent;

            // Event will be null if there are no subscribers. 
            if (handler != null)
            {
                // Use the () operator to raise the event.
                handler(this, e);
            }
        }
        public virtual void Save()
        {
            using (StreamWriter writeRecipeFile = new StreamWriter(_path))
            {
                foreach (IRecipe r in _recipes)
                {
                    writeRecipeFile.WriteLine(SectionRecipe);
                    writeRecipeFile.WriteLine(r.Name);
                    writeRecipeFile.WriteLine(SectionIngredients);

                    foreach (IIngredient ingredient in r.Ingredients)
                    {
                        string writeFileLine = string.Join(";", ingredient.Amount, ingredient.Measure, ingredient.Name );
                        writeRecipeFile.WriteLine(writeFileLine);
                    }

                    writeRecipeFile.WriteLine(SectionInstructions);
                    foreach (string s in r.Instructions)
                    {
                        writeRecipeFile.WriteLine(s);
                    }
                }
            }
            IsModified = false;
            OnRecipesChanged(EventArgs.Empty);
        }
        public virtual void Load()
        {
            List<IRecipe> loadList = new List<IRecipe>();
            RecipeReadStatus statusOfNextLine = RecipeReadStatus.Indefinite;
            string lineFromRecipe = "";
            Recipe recipeForList = null;

            using (StreamReader readRecipes = new StreamReader(_path))
            {
                while ((lineFromRecipe = readRecipes.ReadLine()) != null)
                {
                    if (!String.IsNullOrWhiteSpace(lineFromRecipe))
                    {
                        if (lineFromRecipe == SectionRecipe)
                        {
                            statusOfNextLine = RecipeReadStatus.New;
                            lineFromRecipe = readRecipes.ReadLine();
                        }
                        else if (lineFromRecipe == SectionIngredients)
                        {
                            statusOfNextLine = RecipeReadStatus.Ingredient;
                            lineFromRecipe = readRecipes.ReadLine();
                        }
                        else if (lineFromRecipe == SectionInstructions)
                        {
                            statusOfNextLine = RecipeReadStatus.Instruction;
                            lineFromRecipe = readRecipes.ReadLine();
                        }
                        else
                        {
                            if (statusOfNextLine == RecipeReadStatus.New)
                            {
                                recipeForList = new Recipe(lineFromRecipe);
                                loadList.Add(recipeForList);
                            }
                            else if (statusOfNextLine == RecipeReadStatus.Ingredient)
                            {
                                string[] ingredientsFromRecipe = lineFromRecipe.Split(';');
                                if (ingredientsFromRecipe.Length != 3)
                                {
                                    throw new FileFormatException();
                                }
                                Ingredient ingredient = new Ingredient();
                                ingredient.Name = ingredientsFromRecipe[2];
                                ingredient.Measure = ingredientsFromRecipe[1];
                                ingredient.Amount = ingredientsFromRecipe[0];

                                recipeForList.Add(ingredient);
                            }
                            else if (statusOfNextLine == RecipeReadStatus.Instruction)
                            {
                                recipeForList.Add(lineFromRecipe);
                            }
                            else
                            {
                                throw new FileFormatException();
                            }
                        }
                    }
                }
            }
            loadList.Sort();
            _recipes = loadList;
            IsModified = false;
            OnRecipesChanged(EventArgs.Empty);
        }
    }
}
